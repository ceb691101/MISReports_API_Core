using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.SolarInformation
{
    public class SolarPaymentBulkDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // Using bulk connection
        }

        public List<SolarPaymentBulkModel> GetSolarPaymentBulkReport(SolarPaymentBulkRequest request)
        {
            var results = new List<SolarPaymentBulkModel>();

            try
            {
                logger.Info("=== START GetSolarPaymentBulkReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, NetType={request.NetType}, ReportType={request.ReportType}");

                using (var conn = _dbConnection.GetConnection(true)) // Using bulk connection
                {
                    conn.Open();

                    // Step 1: Get main netmtcons data
                    var bulkData = GetBulkData(conn, request);
                    logger.Info($"Retrieved {bulkData.Count} bulk records");

                    if (bulkData.Count == 0)
                    {
                        logger.Info("No bulk data found");
                        return results;
                    }

                    // Step 2: Get area information (province, region) in batch
                    var areaInfo = GetAreaInformationBatch(conn, bulkData.Select(p => p.AreaCode).Distinct().ToList());
                    logger.Info($"Retrieved area information for {areaInfo.Count} areas");

                    // Step 3: Get customer names in batch
                    var customerNames = GetCustomerNamesBatch(conn, bulkData.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved customer names for {customerNames.Count} accounts");

                    // Step 4: Get charges (mon_tot) in batch
                    var chargesInfo = GetChargesBatch(conn, request.BillCycle, bulkData.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved charges for {chargesInfo.Count} accounts");

                    // Step 5: Get agreement dates in batch
                    var agreementDates = GetAgreementDatesBatch(conn, bulkData.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved agreement dates for {agreementDates.Count} accounts");

                    // Step 6: Combine all data
                    foreach (var bulkRecord in bulkData)
                    {
                        var model = new SolarPaymentBulkModel
                        {
                            AccountNumber = bulkRecord.AccountNumber,
                            PanelCapacity = bulkRecord.PanelCapacity,
                            EnergyExported = bulkRecord.EnergyExported,
                            EnergyImported = bulkRecord.EnergyImported,
                            Tariff = bulkRecord.Tariff,
                            BFUnits = bulkRecord.BFUnits,
                            CFUnits = bulkRecord.CFUnits,
                            Rate = bulkRecord.Rate,
                            UnitSale = bulkRecord.UnitSale,
                            KwhSales = bulkRecord.KwhSales,
                            BankCode = bulkRecord.BankCode,
                            BranchCode = bulkRecord.BranchCode,
                            BankAccountNumber = bulkRecord.BankAccountNumber
                        };

                        // Add area information
                        if (areaInfo.TryGetValue(bulkRecord.AreaCode, out var area))
                        {
                            model.Area = area.AreaName;
                            model.Province = area.ProvinceName;
                            model.Division = area.Region;
                        }
                        else
                        {
                            model.Area = bulkRecord.AreaCode;
                        }

                        // Add customer name
                        if (customerNames.TryGetValue(bulkRecord.AccountNumber, out var customerName))
                        {
                            model.CustomerName = customerName;
                        }
                        else
                        {
                            model.CustomerName = "Unknown";
                        }

                        // Add charges
                        if (chargesInfo.TryGetValue(bulkRecord.AccountNumber, out var charges))
                        {
                            model.KwhCharge = charges.KwhCharge;
                            model.FixedCharge = charges.FixedCharge;
                        }
                        else
                        {
                            model.KwhCharge = 0;
                            model.FixedCharge = 0;
                        }

                        // Add agreement date
                        if (agreementDates.TryGetValue(bulkRecord.AccountNumber, out var agreementDate))
                        {
                            model.AgreementDate = agreementDate;
                        }
                        else
                        {
                            model.AgreementDate = "";
                        }

                        model.ErrorMessage = string.Empty;
                        results.Add(model);
                    }
                }

                logger.Info($"=== END GetSolarPaymentBulkReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching solar payment bulk report");
                throw;
            }
        }

        private List<BulkRawData> GetBulkData(OleDbConnection conn, SolarPaymentBulkRequest request)
        {
            var results = new List<BulkRawData>();
            string sql = BuildBulkQuery(request);

            logger.Debug($"Main query SQL: {sql}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 300; // 5 minutes
                AddParameters(cmd, request);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = new BulkRawData
                        {
                            AreaCode = GetColumnValue(reader, "area_cd"),
                            AccountNumber = GetColumnValue(reader, "acc_nbr"),
                            PanelCapacity = GetDecimalValue(reader, "gen_cap"),
                            EnergyExported = GetIntValue(reader, "exp_kwd_units"),
                            EnergyImported = GetIntValue(reader, "imp_kwd_units"),
                            Tariff = GetColumnValue(reader, "tariff"),
                            BFUnits = GetIntValue(reader, "bf_units"),
                            CFUnits = GetIntValue(reader, "cf_units"),
                            Rate = GetDecimalValue(reader, "rate"),
                            UnitSale = GetIntValue(reader, "unitsale"),
                            KwhSales = GetDecimalValue(reader, "kwh_sales"),
                            BankCode = GetColumnValue(reader, "bank_code"),
                            BranchCode = GetColumnValue(reader, "bran_code"),
                            BankAccountNumber = GetColumnValue(reader, "bk_acc_no")
                        };

                        results.Add(data);
                    }
                }
            }

            return results;
        }

        private Dictionary<string, AreaInformation> GetAreaInformationBatch(OleDbConnection conn, List<string> areaCodes)
        {
            var results = new Dictionary<string, AreaInformation>();

            if (areaCodes == null || areaCodes.Count == 0)
                return results;

            try
            {
                string inClause = string.Join(",", areaCodes.Select((_, i) => $"?"));
                string sql = $@"SELECT a.area_code, a.area_name, p.prov_name, a.region 
                               FROM areas a, provinces p 
                               WHERE a.prov_code = p.prov_code 
                               AND a.area_code IN ({inClause})";

                logger.Debug($"Area info query: {sql}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.CommandTimeout = 300;
                    foreach (var areaCode in areaCodes)
                    {
                        cmd.Parameters.AddWithValue("@areaCode", areaCode);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var areaCode = GetColumnValue(reader, "area_code");
                            if (!string.IsNullOrEmpty(areaCode))
                            {
                                results[areaCode] = new AreaInformation
                                {
                                    AreaName = GetColumnValue(reader, "area_name"),
                                    ProvinceName = GetColumnValue(reader, "prov_name"),
                                    Region = GetColumnValue(reader, "region")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching area information batch");
            }

            return results;
        }

        private Dictionary<string, string> GetCustomerNamesBatch(OleDbConnection conn, List<string> accountNumbers)
        {
            var results = new Dictionary<string, string>();

            if (accountNumbers == null || accountNumbers.Count == 0)
                return results;

            try
            {
                string inClause = string.Join(",", accountNumbers.Select((_, i) => $"?"));
                string sql = $@"SELECT acc_nbr, name 
                               FROM customer 
                               WHERE acc_nbr IN ({inClause})";

                logger.Debug($"Customer names query for {accountNumbers.Count} accounts");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.CommandTimeout = 300;
                    foreach (var accNbr in accountNumbers)
                    {
                        cmd.Parameters.AddWithValue("@accNbr", accNbr);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var accNbr = GetColumnValue(reader, "acc_nbr");
                            var name = GetColumnValue(reader, "name");
                            if (!string.IsNullOrEmpty(accNbr))
                            {
                                results[accNbr] = name?.Trim() ?? "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching customer names batch");
            }

            return results;
        }

        private Dictionary<string, ChargesInformation> GetChargesBatch(OleDbConnection conn, string billCycle, List<string> accountNumbers)
        {
            var results = new Dictionary<string, ChargesInformation>();

            if (accountNumbers == null || accountNumbers.Count == 0)
                return results;

            try
            {
                string inClause = string.Join(",", accountNumbers.Select((_, i) => $"?"));
                string sql = $@"SELECT acc_nbr, tot_kwochg, tot_kwpchg, tot_kwdchg, fixed_chg 
                               FROM mon_tot 
                               WHERE bill_cycle = ? AND acc_nbr IN ({inClause})";

                logger.Debug($"Charges query for {accountNumbers.Count} accounts");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.CommandTimeout = 300;
                    cmd.Parameters.AddWithValue("@billCycle", billCycle);

                    foreach (var accNbr in accountNumbers)
                    {
                        cmd.Parameters.AddWithValue("@accNbr", accNbr);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var accNbr = GetColumnValue(reader, "acc_nbr");
                            if (!string.IsNullOrEmpty(accNbr))
                            {
                                var totKwochg = GetDecimalValue(reader, "tot_kwochg");
                                var totKwpchg = GetDecimalValue(reader, "tot_kwpchg");
                                var totKwdchg = GetDecimalValue(reader, "tot_kwdchg");
                                var fixedChg = GetDecimalValue(reader, "fixed_chg");

                                results[accNbr] = new ChargesInformation
                                {
                                    KwhCharge = totKwochg + totKwpchg + totKwdchg,
                                    FixedCharge = fixedChg
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching charges batch");
            }

            return results;
        }

        private Dictionary<string, string> GetAgreementDatesBatch(OleDbConnection conn, List<string> accountNumbers)
        {
            var results = new Dictionary<string, string>();

            if (accountNumbers == null || accountNumbers.Count == 0)
                return results;

            try
            {
                string inClause = string.Join(",", accountNumbers.Select((_, i) => $"?"));
                string sql = $@"SELECT acc_nbr, agrmnt_date 
                               FROM netmeter 
                               WHERE acc_nbr IN ({inClause})";

                logger.Debug($"Agreement dates query for {accountNumbers.Count} accounts");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.CommandTimeout = 300;
                    foreach (var accNbr in accountNumbers)
                    {
                        cmd.Parameters.AddWithValue("@accNbr", accNbr);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var accNbr = GetColumnValue(reader, "acc_nbr");
                            if (!string.IsNullOrEmpty(accNbr))
                            {
                                var agreementDateStr = GetColumnValue(reader, "agrmnt_date");
                                if (!string.IsNullOrEmpty(agreementDateStr) && DateTime.TryParse(agreementDateStr, out var agreementDate))
                                {
                                    // Check if date is the default "null" date (31-12-1899)
                                    if (agreementDate.Year == 1899 && agreementDate.Month == 12 && agreementDate.Day == 31)
                                    {
                                        results[accNbr] = "";
                                    }
                                    else
                                    {
                                        results[accNbr] = agreementDate.ToString("dd-MM-yyyy");
                                    }
                                }
                                else
                                {
                                    results[accNbr] = "";
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

            return results;
        }

        private string BuildBulkQuery(SolarPaymentBulkRequest request)
        {
            string baseQuery;
            string selectColumns = @"area_cd, acc_nbr, gen_cap, exp_kwd_units, imp_kwd_units, 
                                   tariff, bf_units, cf_units, rate, unitsale, kwh_sales, 
                                   bank_code, bran_code, bk_acc_no";

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = $@"SELECT {selectColumns} 
                                  FROM netmtcons 
                                  WHERE area_cd=? AND net_type=? AND bill_cycle=? 
                                  ORDER BY acc_nbr";
                    break;

                case SolarReportType.Province:
                    baseQuery = $@"SELECT n.{selectColumns.Replace(",", ",n.")} 
                                  FROM netmtcons n, areas a 
                                  WHERE n.net_type=? AND n.bill_cycle=? 
                                  AND n.area_cd = a.area_code AND a.prov_code=? 
                                  ORDER BY n.area_cd, n.acc_nbr";
                    break;

                case SolarReportType.Region:
                    baseQuery = $@"SELECT n.{selectColumns.Replace(",", ",n.")} 
                                  FROM netmtcons n, areas a 
                                  WHERE n.net_type=? AND n.bill_cycle=? 
                                  AND n.area_cd = a.area_code AND a.region=? 
                                  ORDER BY n.area_cd, n.acc_nbr";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = $@"SELECT {selectColumns} 
                                  FROM netmtcons 
                                  WHERE net_type=? AND bill_cycle=? 
                                  ORDER BY acc_nbr";
                    break;
            }

            return baseQuery;
        }

        private void AddParameters(OleDbCommand cmd, SolarPaymentBulkRequest request)
        {
            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    cmd.Parameters.AddWithValue("@areaCode", request.AreaCode);
                    cmd.Parameters.AddWithValue("@netType", request.NetType);
                    cmd.Parameters.AddWithValue("@billCycle", request.BillCycle);
                    break;
                case SolarReportType.Province:
                    cmd.Parameters.AddWithValue("@netType", request.NetType);
                    cmd.Parameters.AddWithValue("@billCycle", request.BillCycle);
                    cmd.Parameters.AddWithValue("@provCode", request.ProvCode);
                    break;
                case SolarReportType.Region:
                    cmd.Parameters.AddWithValue("@netType", request.NetType);
                    cmd.Parameters.AddWithValue("@billCycle", request.BillCycle);
                    cmd.Parameters.AddWithValue("@region", request.Region);
                    break;
                case SolarReportType.EntireCEB:
                default:
                    cmd.Parameters.AddWithValue("@netType", request.NetType);
                    cmd.Parameters.AddWithValue("@billCycle", request.BillCycle);
                    break;
            }
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

        private int GetIntValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid int format in column '{columnName}'");
                return 0;
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

        // Helper classes for batch processing
        private class BulkRawData
        {
            public string AreaCode { get; set; }
            public string AccountNumber { get; set; }
            public decimal PanelCapacity { get; set; }
            public int EnergyExported { get; set; }
            public int EnergyImported { get; set; }
            public string Tariff { get; set; }
            public int BFUnits { get; set; }
            public int CFUnits { get; set; }
            public decimal Rate { get; set; }
            public int UnitSale { get; set; }
            public decimal KwhSales { get; set; }
            public string BankCode { get; set; }
            public string BranchCode { get; set; }
            public string BankAccountNumber { get; set; }
        }

        private class AreaInformation
        {
            public string AreaName { get; set; }
            public string ProvinceName { get; set; }
            public string Region { get; set; }
        }

        private class ChargesInformation
        {
            public decimal KwhCharge { get; set; }
            public decimal FixedCharge { get; set; }
        }
    }
}