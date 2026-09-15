using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.SolarInformation.SolarConnectionDetails
{
    public class SolarReadingUsageBulkDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // Use bulk connection
        }

        public List<SolarReadingUsageBulkModel> GetSolarReadingUsageBulkReport(BulkUsageRequest request)
        {
            var results = new List<SolarReadingUsageBulkModel>();

            try
            {
                logger.Info("=== START GetSolarReadingUsageBulkReport ===");
                logger.Info($"Request: AddedBillCycle={request.AddedBillCycle}, BillCycle={request.BillCycle}, NetType={request.NetType}, ReportType={request.ReportType}");

                using (var conn = _dbConnection.GetConnection(true)) // Use bulk connection
                {
                    conn.Open();

                    // Step 1: Get main export readings data (mtr_seq=2) with customer info
                    var exportReadings = GetExportReadingsData(conn, request);
                    logger.Info($"Retrieved {exportReadings.Count} export readings records");

                    if (exportReadings.Count == 0)
                    {
                        logger.Info("No export readings data found");
                        return results;
                    }

                    // Step 2: Get import readings for each account (mtr_seq=1)
                    var importReadings = GetImportReadingsBatch(conn, request.AddedBillCycle,
                        exportReadings.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved import readings for {importReadings.Count} accounts");

                    // Step 3: Get unit cost data from netmtcons
                    var unitCostData = GetUnitCostDataBatch(conn, request.BillCycle,
                        exportReadings.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved unit cost data for {unitCostData.Count} accounts");

                    // Step 4: Get area information (province, region) in batch
                    var areaInfo = GetAreaInformationBatch(conn,
                        exportReadings.Select(p => p.AreaCode).Distinct().ToList());
                    logger.Info($"Retrieved area information for {areaInfo.Count} areas");

                    // Step 5: Get agreement dates from netmeter table
                    var agreementDates = GetAgreementDatesBatch(conn,
                        exportReadings.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved agreement dates for {agreementDates.Count} accounts");

                    // Step 6: Combine all data
                    foreach (var exportRecord in exportReadings)
                    {
                        var model = new SolarReadingUsageBulkModel
                        {
                            AreaCode = exportRecord.AreaCode,
                            AccountNumber = exportRecord.AccountNumber,
                            Name = exportRecord.Name,
                            Tariff = exportRecord.Tariff,
                            NetType = exportRecord.NetType,
                            MeterNumber = exportRecord.MeterNumber,
                            PresentReadingDate = exportRecord.PresentReadingDate,
                            PreviousReadingDate = exportRecord.PreviousReadingDate,
                            PresentReadingExport = exportRecord.PresentReadingExport,
                            PreviousReadingExport = exportRecord.PreviousReadingExport,
                            UnitsOut = exportRecord.UnitsOut,
                            BillCycle = request.BillCycle
                        };

                        // Add import readings
                        if (importReadings.TryGetValue(exportRecord.AccountNumber, out var importData))
                        {
                            model.PresentReadingImport = importData.PresentReadingImport;
                            model.PreviousReadingImport = importData.PreviousReadingImport;
                            model.UnitsIn = importData.UnitsIn;
                        }

                        // Add unit cost data
                        if (unitCostData.TryGetValue(exportRecord.AccountNumber, out var costData))
                        {
                            model.UnitCost = costData.Rate;
                            model.NetUnits = costData.UnitSale;
                        }
                        else
                        {
                            model.NetUnits = 0; // Default value if no data found
                        }


                        // Add area information
                        if (areaInfo.TryGetValue(exportRecord.AreaCode, out var area))
                        {
                            model.Area = area.AreaName;
                            model.Province = area.ProvinceName;
                            model.Division = area.Region;
                        }
                        else
                        {
                            model.Area = exportRecord.AreaCode;
                        }

                        // Add agreement date
                        if (agreementDates.TryGetValue(exportRecord.AccountNumber, out var agrmntDate))
                        {
                            model.AgreementDate = agrmntDate;
                        }

                        model.ErrorMessage = string.Empty;
                        results.Add(model);
                    }
                }

                logger.Info($"=== END GetSolarReadingUsageBulkReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching solar reading usage bulk report");
                throw;
            }
        }

        /// <summary>
        /// Gets export readings data (mtr_seq=2) with customer info based on report type
        /// </summary>
        private List<ExportReadingData> GetExportReadingsData(OleDbConnection conn, BulkUsageRequest request)
        {
            var results = new List<ExportReadingData>();

            try
            {
                string sql = BuildMainQuerySql(request.ReportType);

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    // Add parameters based on report type
                    cmd.Parameters.AddWithValue("@added_blcy", request.AddedBillCycle);
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);

                    switch (request.ReportType)
                    {
                        case SolarReportType.Area:
                            cmd.Parameters.AddWithValue("@area_cd", request.AreaCode);
                            cmd.Parameters.AddWithValue("@net_type", request.NetType);
                            break;
                        case SolarReportType.Province:
                            cmd.Parameters.AddWithValue("@prov_code", request.ProvCode);
                            cmd.Parameters.AddWithValue("@net_type", request.NetType);
                            break;
                        case SolarReportType.Region:
                            cmd.Parameters.AddWithValue("@region", request.Region);
                            cmd.Parameters.AddWithValue("@net_type", request.NetType);
                            break;
                        case SolarReportType.EntireCEB:
                            cmd.Parameters.AddWithValue("@net_type", request.NetType);
                            break;
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = new ExportReadingData
                            {
                                AreaCode = GetColumnValue(reader, "area_cd"),
                                AccountNumber = GetColumnValue(reader, "acc_nbr"),
                                Name = GetColumnValue(reader, "name")?.Trim() ?? "",
                                Tariff = GetColumnValue(reader, "tariff"),
                                NetType = GetColumnValue(reader, "net_type"),
                                MeterNumber = GetColumnValue(reader, "mtr_nbr"),
                                PresentReadingDate = FormatDate(GetColumnValue(reader, "rdng_date")),
                                PreviousReadingDate = FormatDate(GetColumnValue(reader, "prv_date")),
                                PresentReadingExport = GetColumnValue(reader, "rdn"),
                                PreviousReadingExport = GetColumnValue(reader, "prv_rdn"),
                                UnitsOut = GetColumnValue(reader, "units")
                            };

                            results.Add(data);
                        }
                    }
                }

                logger.Info($"Retrieved {results.Count} export reading records");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching export readings data");
            }

            return results;
        }

        /// <summary>
        /// Builds the main SQL query based on report type
        /// </summary>
        private string BuildMainQuerySql(SolarReportType reportType)
        {
            string baseSql = @"SELECT c.area_cd, c.acc_nbr, c.name, c.tariff, r.mtr_nbr, 
                              r.rdng_date, r.prv_date, r.rdn, r.prv_rdn, r.units, m.net_type 
                              FROM customer c, rdngs r, netmtcons m ";

            switch (reportType)
            {
                case SolarReportType.Area:
                    return baseSql + @"WHERE c.acc_nbr = r.acc_nbr 
                                      AND r.added_blcy=? 
                                      AND r.mtr_type='KWD' 
                                      AND r.mtr_seq='2' 
                                      AND m.bill_cycle=? 
                                      AND c.area_cd=? 
                                      AND m.net_type=? 
                                      AND c.acc_nbr = m.acc_nbr 
                                      ORDER BY c.acc_nbr, r.mtr_seq";

                case SolarReportType.Province:
                    return baseSql + @", areas a 
                                      WHERE c.acc_nbr = r.acc_nbr 
                                      AND r.added_blcy=? 
                                      AND r.mtr_type='KWD' 
                                      AND r.mtr_seq='2' 
                                      AND m.bill_cycle=? 
                                      AND c.area_cd = a.area_code 
                                      AND a.prov_code=? 
                                      AND m.net_type=? 
                                      AND c.acc_nbr = m.acc_nbr 
                                      ORDER BY c.acc_nbr, r.mtr_seq";

                case SolarReportType.Region:
                    return baseSql + @", areas a 
                                      WHERE c.acc_nbr = r.acc_nbr 
                                      AND r.added_blcy=? 
                                      AND r.mtr_type='KWD' 
                                      AND r.mtr_seq='2' 
                                      AND m.bill_cycle=? 
                                      AND c.area_cd = a.area_code 
                                      AND a.region=? 
                                      AND m.net_type=? 
                                      AND c.acc_nbr = m.acc_nbr 
                                      ORDER BY c.acc_nbr, r.mtr_seq";

                case SolarReportType.EntireCEB:
                    return baseSql + @", areas a 
                                      WHERE c.acc_nbr = r.acc_nbr 
                                      AND r.added_blcy=? 
                                      AND r.mtr_type='KWD' 
                                      AND r.mtr_seq='2' 
                                      AND m.bill_cycle=? 
                                      AND c.area_cd = a.area_code 
                                      AND m.net_type=? 
                                      AND c.acc_nbr = m.acc_nbr 
                                      ORDER BY c.acc_nbr, r.mtr_seq";

                default:
                    throw new ArgumentException($"Unsupported report type: {reportType}");
            }
        }

        /// <summary>
        /// Gets import readings (mtr_seq=1) for multiple accounts in batch
        /// </summary>
        private Dictionary<string, ImportReadingData> GetImportReadingsBatch(OleDbConnection conn,
            string addedBillCycle, List<string> accountNumbers)
        {
            var result = new Dictionary<string, ImportReadingData>();

            if (accountNumbers == null || accountNumbers.Count == 0)
                return result;

            try
            {
                // Process in batches to avoid parameter limit
                const int batchSize = 500;
                for (int i = 0; i < accountNumbers.Count; i += batchSize)
                {
                    var batch = accountNumbers.Skip(i).Take(batchSize).ToList();
                    var parameters = string.Join(",", batch.Select((_, idx) => $"?"));

                    string sql = $@"SELECT acc_nbr, rdn, prv_rdn, units 
                                   FROM rdngs 
                                   WHERE acc_nbr IN ({parameters}) 
                                   AND added_blcy=? 
                                   AND mtr_type='KWD' 
                                   AND mtr_seq='1'";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        foreach (var acctNum in batch)
                        {
                            cmd.Parameters.AddWithValue("?", acctNum);
                        }
                        cmd.Parameters.AddWithValue("?", addedBillCycle);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var acctNumber = GetColumnValue(reader, "acc_nbr");
                                if (!string.IsNullOrEmpty(acctNumber))
                                {
                                    var data = new ImportReadingData
                                    {
                                        PresentReadingImport = GetColumnValue(reader, "rdn"),
                                        PreviousReadingImport = GetColumnValue(reader, "prv_rdn"),
                                        UnitsIn = GetColumnValue(reader, "units")
                                    };

                                    result[acctNumber] = data;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching import readings batch");
            }

            return result;
        }

        /// <summary>
        /// Gets unit cost data from netmtcons for multiple accounts in batch
        /// </summary>
        private Dictionary<string, UnitCostData> GetUnitCostDataBatch(OleDbConnection conn,
            string billCycle, List<string> accountNumbers)
        {
            var result = new Dictionary<string, UnitCostData>();

            if (accountNumbers == null || accountNumbers.Count == 0)
                return result;

            try
            {
                // Process in batches to avoid parameter limit
                const int batchSize = 500;
                for (int i = 0; i < accountNumbers.Count; i += batchSize)
                {
                    var batch = accountNumbers.Skip(i).Take(batchSize).ToList();
                    var parameters = string.Join(",", batch.Select((_, idx) => $"?"));

                    string sql = $@"SELECT acc_nbr, rate, unitsale 
                                   FROM netmtcons 
                                   WHERE acc_nbr IN ({parameters}) 
                                   AND bill_cycle=?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        foreach (var acctNum in batch)
                        {
                            cmd.Parameters.AddWithValue("?", acctNum);
                        }
                        cmd.Parameters.AddWithValue("?", billCycle);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var acctNumber = GetColumnValue(reader, "acc_nbr");
                                if (!string.IsNullOrEmpty(acctNumber))
                                {
                                    var data = new UnitCostData
                                    {
                                        Rate = GetDecimalValue(reader, "rate"),
                                        UnitSale = GetDecimalValue(reader, "unitsale")
                                    };

                                    result[acctNumber] = data;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching unit cost data batch");
            }

            return result;
        }

        /// <summary>
        /// Gets area information (province, region) for multiple areas in batch
        /// </summary>
        private Dictionary<string, AreaInformation> GetAreaInformationBatch(OleDbConnection conn,
            List<string> areaCodes)
        {
            var result = new Dictionary<string, AreaInformation>();

            if (areaCodes == null || areaCodes.Count == 0)
                return result;

            try
            {
                var parameters = string.Join(",", areaCodes.Select((_, idx) => $"?"));

                string sql = $@"SELECT a.area_code, a.area_name, p.prov_name, a.region 
                               FROM areas a, provinces p 
                               WHERE a.prov_code = p.prov_code 
                               AND a.area_code IN ({parameters})";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    foreach (var areaCode in areaCodes)
                    {
                        cmd.Parameters.AddWithValue("?", areaCode);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var areaCode = GetColumnValue(reader, "area_code");
                            if (!string.IsNullOrEmpty(areaCode))
                            {
                                var info = new AreaInformation
                                {
                                    AreaName = GetColumnValue(reader, "area_name")?.Trim() ?? "",
                                    ProvinceName = GetColumnValue(reader, "prov_name")?.Trim() ?? "",
                                    Region = GetColumnValue(reader, "region")?.Trim() ?? ""
                                };

                                result[areaCode] = info;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching area information batch");
            }

            return result;
        }

        /// <summary>
        /// Gets agreement dates from netmeter table for multiple accounts in batch
        /// </summary>
        private Dictionary<string, string> GetAgreementDatesBatch(OleDbConnection conn,
            List<string> accountNumbers)
        {
            var result = new Dictionary<string, string>();

            if (accountNumbers == null || accountNumbers.Count == 0)
                return result;

            try
            {
                // Process in batches to avoid parameter limit
                const int batchSize = 500;
                for (int i = 0; i < accountNumbers.Count; i += batchSize)
                {
                    var batch = accountNumbers.Skip(i).Take(batchSize).ToList();
                    var parameters = string.Join(",", batch.Select((_, idx) => $"?"));

                    string sql = $@"SELECT acc_nbr, agrmnt_date 
                                   FROM netmeter 
                                   WHERE acc_nbr IN ({parameters})";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        foreach (var acctNum in batch)
                        {
                            cmd.Parameters.AddWithValue("?", acctNum);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var acctNumber = GetColumnValue(reader, "acc_nbr");
                                if (!string.IsNullOrEmpty(acctNumber))
                                {
                                    var agrmntDate = GetColumnValue(reader, "agrmnt_date");
                                    if (!string.IsNullOrEmpty(agrmntDate))
                                    {
                                        result[acctNumber] = agrmntDate.Trim();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching agreement dates batch");
            }

            return result;
        }

        // Helper methods
        private string GetColumnValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? null : value.ToString()?.Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return null;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column '{columnName}'");
                return 0;
            }
        }

        private string FormatDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return "";

            if (DateTime.TryParse(dateStr, out var date))
            {
                return date.ToString("dd-MM-yyyy");
            }

            return dateStr;
        }

        // Helper classes for batch processing
        private class ExportReadingData
        {
            public string AreaCode { get; set; }
            public string AccountNumber { get; set; }
            public string Name { get; set; }
            public string Tariff { get; set; }
            public string NetType { get; set; }
            public string MeterNumber { get; set; }
            public string PresentReadingDate { get; set; }
            public string PreviousReadingDate { get; set; }
            public string PresentReadingExport { get; set; }
            public string PreviousReadingExport { get; set; }
            public string UnitsOut { get; set; }
        }

        private class ImportReadingData
        {
            public string PresentReadingImport { get; set; }
            public string PreviousReadingImport { get; set; }
            public string UnitsIn { get; set; }
        }

        private class UnitCostData
        {
            public decimal Rate { get; set; }
            public decimal UnitSale { get; set; }
        }

        private class AreaInformation
        {
            public string AreaName { get; set; }
            public string ProvinceName { get; set; }
            public string Region { get; set; }
        }
    }
}