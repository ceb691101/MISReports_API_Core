using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.SolarInformation.SolarConnectionDetails
{
    public class SolarReadingRetailDetailedDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        public List<SolarReadingDetailedModel> GetSolarReadingDetailedReport(RetailDetailedRequest request)
        {
            var results = new List<SolarReadingDetailedModel>();

            try
            {
                logger.Info("=== START GetSolarReadingDetailedReport ===");
                logger.Info($"Request: {request.CycleType}Cycle={request.BillCycle ?? request.CalcCycle}, NetType={request.NetType}, ReportType={request.ReportType}");

                // Get bill cycle for customer lookups
                string billCycleForCustomer = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Step 1: Validate cycle completion if needed
                    bool cycleCompleted = ValidateCycleCompletion(conn, request, billCycleForCustomer);
                    if (!cycleCompleted)
                    {
                        logger.Warn("Cycle not completed");
                        // You can choose to return empty or continue based on business logic
                    }

                    // Step 2: Get main netmtcons data
                    var netmtconsData = GetNetMtConsData(conn, request);
                    logger.Info($"Retrieved {netmtconsData.Count} netmtcons records");

                    if (netmtconsData.Count == 0)
                    {
                        logger.Info("No netmtcons data found");
                        return results;
                    }

                    // Step 3: Get area information (province, region) in batch
                    var areaInfo = GetAreaInformationBatch(conn, netmtconsData.Select(p => p.AreaCode).Distinct().ToList());
                    logger.Info($"Retrieved area information for {areaInfo.Count} areas");

                    // Step 4: Get customer information in batch (from prn_dat_1)
                    var customerInfo = GetCustomerInformationBatch(conn, billCycleForCustomer, netmtconsData.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved customer information for {customerInfo.Count} accounts");

                    // Step 5: Combine all data
                    foreach (var netRecord in netmtconsData)
                    {
                        var model = new SolarReadingDetailedModel
                        {
                            AccountNumber = netRecord.AccountNumber,
                            UnitsIn = netRecord.UnitsIn,
                            UnitsOut = netRecord.UnitsOut,
                            UnitCost = netRecord.Rate,
                            NetUnits = netRecord.UnitSale,
                            PayableAmount = netRecord.KwhSales,
                            BankCode = netRecord.BankCode,
                            BranchCode = netRecord.BranchCode,
                            BankAccountNumber = netRecord.BankAccountNumber,
                            AgreementDate = netRecord.AgreementDate,
                            BillCycle = netRecord.BillCycle,
                            AreaCode = netRecord.AreaCode
                        };

                        // Calculate Net Units (UnitsIn - UnitsOut)
                        //model.NetUnits = model.UnitsIn - model.UnitsOut;

                        // Add area information
                        if (areaInfo.TryGetValue(netRecord.AreaCode, out var area))
                        {
                            model.Area = area.AreaName;
                            model.Province = area.ProvinceName;
                            model.Division = area.Region;
                        }
                        else
                        {
                            model.Area = netRecord.AreaCode;
                        }

                        // Add customer information
                        if (customerInfo.TryGetValue(netRecord.AccountNumber, out var customer))
                        {
                            model.Name = customer.CustomerName;
                            model.Tariff = customer.Tariff;
                            model.MeterNumber = customer.MeterNumber;
                            model.PresentReadingDate = customer.PresentReadingDate;
                            model.PreviousReadingDate = customer.PreviousReadingDate;
                            model.PresentReadingImport = customer.PresentReadingImport;
                            model.PreviousReadingImport = customer.PreviousReadingImport;
                            model.PresentReadingExport = customer.PresentReadingExport;
                            model.PreviousReadingExport = customer.PreviousReadingExport;
                        }
                        else
                        {
                            model.Name = "Unknown";
                        }

                        model.ErrorMessage = string.Empty;
                        results.Add(model);
                    }
                }

                logger.Info($"=== END GetSolarReadingDetailedReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching solar reading detailed report");
                throw;
            }
        }

        /// <summary>
        /// Validates if the cycle is completed based on the first 3 characters of bill cycle
        /// </summary>
        private bool ValidateCycleCompletion(OleDbConnection conn, RetailDetailedRequest request, string billCycle)
        {
            try
            {
                if (string.IsNullOrEmpty(billCycle) || billCycle.Length < 3)
                    return true; // Skip validation if cycle format is invalid

                string cycle1 = billCycle.Substring(0, 3).Trim();
                string cycle2 = string.Empty;

                string sql = string.Empty;

                switch (request.ReportType)
                {
                    case SolarReportType.Area:
                        sql = "SELECT bill_cycle FROM areas WHERE area_code=?";
                        break;
                    case SolarReportType.Province:
                        sql = "SELECT MIN(bill_cycle) FROM areas WHERE prov_code=?";
                        break;
                    case SolarReportType.Region:
                        sql = "SELECT MIN(bill_cycle) FROM areas WHERE region=?";
                        break;
                    case SolarReportType.EntireCEB:
                        sql = "SELECT MIN(bill_cycle) FROM areas";
                        break;
                }

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    // Add parameters based on report type
                    switch (request.ReportType)
                    {
                        case SolarReportType.Area:
                            cmd.Parameters.AddWithValue("@areaCode", request.AreaCode);
                            break;
                        case SolarReportType.Province:
                            cmd.Parameters.AddWithValue("@provCode", request.ProvCode);
                            break;
                        case SolarReportType.Region:
                            cmd.Parameters.AddWithValue("@region", request.Region);
                            break;
                    }

                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        cycle2 = result.ToString().Trim();
                        if (cycle2.Length >= 3)
                        {
                            cycle2 = cycle2.Substring(0, 3);
                        }
                    }
                }

                // Compare cycles
                if (string.Compare(cycle1, cycle2, StringComparison.Ordinal) > 0)
                {
                    logger.Warn($"Cycle not completed. cycle1={cycle1}, cycle2={cycle2}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error validating cycle completion");
                return true; // Continue processing if validation fails
            }
        }

        /// <summary>
        /// Gets netmtcons data based on report type and cycle type
        /// Handles the special case where net_type='2' should also include net_type='5'
        /// </summary>
        private List<NetMtConsRawData> GetNetMtConsData(OleDbConnection conn, RetailDetailedRequest request)
        {
            var results = new List<NetMtConsRawData>();
            string sql = BuildNetMtConsQuery(request);

            logger.Debug($"NetMtCons query SQL: {sql}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 300; // 5 minutes
                AddParameters(cmd, request);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = new NetMtConsRawData
                        {
                            AreaCode = GetColumnValue(reader, "area_code"),
                            AccountNumber = GetColumnValue(reader, "acct_number"),
                            UnitsIn = GetDecimalValue(reader, "units_in"),
                            UnitsOut = GetDecimalValue(reader, "units_out"),
                            UnitSale = GetIntValue(reader, "unitsale"),
                            Rate = GetDecimalValue(reader, "rate"),
                            KwhSales = GetDecimalValue(reader, "kwh_sales"),
                            BillCycle = GetColumnValue(reader, "bill_cycle"),
                            BankCode = GetColumnValue(reader, "bank_code"),
                            BranchCode = GetColumnValue(reader, "bran_code"),
                            BankAccountNumber = GetColumnValue(reader, "bk_ac_no")
                        };

                        // Handle agreement date
                        var agreementDateStr = GetColumnValue(reader, "agrmnt_date");
                        if (!string.IsNullOrEmpty(agreementDateStr) && DateTime.TryParse(agreementDateStr, out var agreementDate))
                        {
                            // Check if date is the default "null" date (31-12-1899)
                            if (agreementDate.Year == 1899 && agreementDate.Month == 12 && agreementDate.Day == 31)
                            {
                                data.AgreementDate = "";
                            }
                            else
                            {
                                data.AgreementDate = agreementDate.ToString("dd-MM-yyyy");
                            }
                        }
                        else
                        {
                            data.AgreementDate = "";
                        }
                        results.Add(data);
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Builds the query for netmtcons table based on report type and cycle type
        /// </summary>
        private string BuildNetMtConsQuery(RetailDetailedRequest request)
        {
            string cycleField = request.CycleType == "A" ? "bill_cycle" : "calc_cycle";

            string selectColumns = @"area_code,acct_number,units_in,units_out,unitsale,rate,
                                    kwh_sales,bill_cycle,bank_code,bran_code,bk_ac_no,agrmnt_date";

            string netTypeCondition = BuildNetTypeCondition(request.NetType);
            string baseQuery = string.Empty;

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = $@"SELECT {selectColumns} 
                                  FROM netmtcons 
                                  WHERE area_code=? AND {netTypeCondition} AND {cycleField}=? 
                                  ORDER BY area_code, acct_number";
                    break;

                case SolarReportType.Province:
                    baseQuery = $@"SELECT n.{selectColumns.Replace(",", ",n.")} 
                                  FROM netmtcons n, areas a 
                                  WHERE n.area_code = a.area_code 
                                  AND a.prov_code=? AND {netTypeCondition.Replace("net_type", "n.net_type")} AND n.{cycleField}=? 
                                  ORDER BY n.area_code, n.acct_number";
                    break;

                case SolarReportType.Region:
                    baseQuery = $@"SELECT n.{selectColumns.Replace(",", ",n.")} 
                                  FROM netmtcons n, areas a 
                                  WHERE n.area_code = a.area_code 
                                  AND a.region=? AND {netTypeCondition.Replace("net_type", "n.net_type")} AND n.{cycleField}=? 
                                  ORDER BY n.area_code, n.acct_number";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = $@"SELECT {selectColumns} 
                                  FROM netmtcons 
                                  WHERE {netTypeCondition} AND {cycleField}=? 
                                  ORDER BY area_code, acct_number";
                    break;
            }

            return baseQuery;
        }

        /// <summary>
        /// Builds the net_type condition. 
        /// Special case: if net_type='2', include both '2' and '5'
        /// </summary>
        private string BuildNetTypeCondition(string netType)
        {
            if (netType == "2")
            {
                return "(net_type='2' OR net_type='5')";
            }
            else
            {
                return "net_type=?";
            }
        }

        private void AddParameters(OleDbCommand cmd, RetailDetailedRequest request)
        {
            string cycleValue = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    cmd.Parameters.AddWithValue("@areaCode", request.AreaCode);
                    // Only add netType parameter if not using the special '2' or '5' condition
                    if (request.NetType != "2")
                    {
                        cmd.Parameters.AddWithValue("@netType", request.NetType);
                    }
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.Province:
                    cmd.Parameters.AddWithValue("@provCode", request.ProvCode);
                    if (request.NetType != "2")
                    {
                        cmd.Parameters.AddWithValue("@netType", request.NetType);
                    }
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.Region:
                    cmd.Parameters.AddWithValue("@region", request.Region);
                    if (request.NetType != "2")
                    {
                        cmd.Parameters.AddWithValue("@netType", request.NetType);
                    }
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.EntireCEB:
                default:
                    if (request.NetType != "2")
                    {
                        cmd.Parameters.AddWithValue("@netType", request.NetType);
                    }
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
            }
        }

        /// <summary>
        /// Gets area information (province, region) for multiple area codes in batch
        /// </summary>
        private Dictionary<string, AreaInformation> GetAreaInformationBatch(OleDbConnection conn, List<string> areaCodes)
        {
            var result = new Dictionary<string, AreaInformation>();

            if (areaCodes == null || areaCodes.Count == 0)
                return result;

            try
            {
                // Create parameterized query with IN clause
                var parameters = string.Join(",", areaCodes.Select((_, i) => $"?"));
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
                                result[areaCode] = new AreaInformation
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

            return result;
        }

        /// <summary>
        /// Gets customer information from prn_dat_1 for multiple accounts in batch
        /// </summary>
        private Dictionary<string, CustomerInformation> GetCustomerInformationBatch(OleDbConnection conn, string billCycle, List<string> accountNumbers)
        {
            var result = new Dictionary<string, CustomerInformation>();

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

                    string sql = $@"SELECT acct_number, cust_fname, cust_lname, tariff_code, met_no1,
                                   crnt_rd_dt, prvs_rd_dt, crnt_rd1, prvs_rd1, crnt_rd2, prvs_rd2
                                   FROM prn_dat_1 
                                   WHERE bill_cycle=? AND acct_number IN ({parameters})";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        foreach (var acctNum in batch)
                        {
                            cmd.Parameters.AddWithValue("?", acctNum);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var acctNumber = GetColumnValue(reader, "acct_number");
                                if (!string.IsNullOrEmpty(acctNumber))
                                {
                                    var firstName = GetColumnValue(reader, "cust_fname")?.Trim() ?? "";
                                    var lastName = GetColumnValue(reader, "cust_lname")?.Trim() ?? "";
                                    var customerName = $"{firstName} {lastName}".Trim();

                                    var info = new CustomerInformation
                                    {
                                        CustomerName = customerName,
                                        Tariff = GetColumnValue(reader, "tariff_code"),
                                        MeterNumber = GetColumnValue(reader, "met_no1"),
                                        PresentReadingDate = FormatDate(GetColumnValue(reader, "crnt_rd_dt")),
                                        PreviousReadingDate = FormatDate(GetColumnValue(reader, "prvs_rd_dt")),
                                        PresentReadingImport = GetDecimalValue(reader, "crnt_rd1"),
                                        PreviousReadingImport = GetDecimalValue(reader, "prvs_rd1"),
                                        PresentReadingExport = GetDecimalValue(reader, "crnt_rd2"),
                                        PreviousReadingExport = GetDecimalValue(reader, "prvs_rd2")
                                    };

                                    result[acctNumber] = info;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching customer information batch");
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
        private class NetMtConsRawData
        {
            public string AreaCode { get; set; }
            public string AccountNumber { get; set; }
            public decimal UnitsIn { get; set; }
            public decimal UnitsOut { get; set; }
            public int UnitSale { get; set; }
            public decimal Rate { get; set; }
            public decimal KwhSales { get; set; }
            public string BillCycle { get; set; }
            public string BankCode { get; set; }
            public string BranchCode { get; set; }
            public string BankAccountNumber { get; set; }
            public string AgreementDate { get; set; }
        }

        private class AreaInformation
        {
            public string AreaName { get; set; }
            public string ProvinceName { get; set; }
            public string Region { get; set; }
        }

        private class CustomerInformation
        {
            public string CustomerName { get; set; }
            public string Tariff { get; set; }
            public string MeterNumber { get; set; }
            public string PresentReadingDate { get; set; }
            public string PreviousReadingDate { get; set; }
            public decimal PresentReadingImport { get; set; }
            public decimal PreviousReadingImport { get; set; }
            public decimal PresentReadingExport { get; set; }
            public decimal PreviousReadingExport { get; set; }
        }
    }
}