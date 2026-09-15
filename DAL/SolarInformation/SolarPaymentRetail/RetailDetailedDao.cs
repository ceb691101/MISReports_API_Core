using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.SolarInformation.SolarPaymentRetail
{
    public class RetailDetailedDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        public List<RetailDetailedModel> GetRetailDetailedReport(RetailDetailedRequest request)
        {
            var results = new List<RetailDetailedModel>();

            try
            {
                logger.Info("=== START GetRetailDetailedReport ===");
                logger.Info($"Request: {request.CycleType}Cycle={request.BillCycle ?? request.CalcCycle}, NetType={request.NetType}, ReportType={request.ReportType}");

                // Get bill cycle for customer lookups
                string billCycleForCustomer = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Step 1: Get main retail data
                    var retailData = GetRetailData(conn, request);
                    logger.Info($"Retrieved {retailData.Count} retail records");

                    if (retailData.Count == 0)
                    {
                        logger.Info("No retail data found");
                        return results;
                    }

                    // Step 2: Get area information (province, region) in batch
                    var areaInfo = GetAreaInformationBatch(conn, retailData.Select(p => p.AreaCode).Distinct().ToList());
                    logger.Info($"Retrieved area information for {areaInfo.Count} areas");

                    // Step 3: Get customer information in batch
                    var customerInfo = GetCustomerInformationBatch(conn, billCycleForCustomer, retailData.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved customer information for {customerInfo.Count} accounts");

                    // Step 4: Combine all data
                    foreach (var retailRecord in retailData)
                    {
                        var model = new RetailDetailedModel
                        {
                            AccountNumber = retailRecord.AccountNumber,
                            PanelCapacity = retailRecord.PanelCapacity,
                            EnergyExported = retailRecord.EnergyExported,
                            EnergyImported = retailRecord.EnergyImported,
                            Tariff = retailRecord.Tariff,
                            BFUnits = retailRecord.BFUnits,
                            UnitsInBill = retailRecord.UnitsInBill,
                            Period = retailRecord.Period,
                            KwhCharge = retailRecord.KwhCharge,
                            FixedCharge = retailRecord.FixedCharge,
                            FuelCharge = retailRecord.FuelCharge,
                            CFUnits = retailRecord.CFUnits,
                            Rate = retailRecord.Rate,
                            UnitSale = retailRecord.UnitSale,
                            KwhSales = retailRecord.KwhSales,
                            BankCode = retailRecord.BankCode,
                            BranchCode = retailRecord.BranchCode,
                            BankAccountNumber = retailRecord.BankAccountNumber,
                            AgreementDate = retailRecord.AgreementDate,
                        };

                        // Add area information
                        if (areaInfo.TryGetValue(retailRecord.AreaCode, out var area))
                        {
                            model.Area = area.AreaName;
                            model.Province = area.ProvinceName;
                            model.Division = area.Region;
                        }
                        else
                        {
                            model.Area = retailRecord.AreaCode;
                        }

                        // Add customer information
                        if (customerInfo.TryGetValue(retailRecord.AccountNumber, out var customer))
                        {
                            model.CustomerName = customer.CustomerName;
                            
                        }
                        else
                        {
                            model.CustomerName = "Unknown";
                            
                        }

                        model.ErrorMessage = string.Empty;
                        results.Add(model);
                    }
                }

                logger.Info($"=== END GetRetailDetailedReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching retail detailed report");
                throw;
            }
        }

        private List<RetailRawData> GetRetailData(OleDbConnection conn, RetailDetailedRequest request)
        {
            var results = new List<RetailRawData>();
            string sql = BuildRetailQuery(request);

            logger.Debug($"Main query SQL: {sql}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 300; // 5 minutes
                AddParameters(cmd, request);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = new RetailRawData
                        {
                            AreaCode = GetColumnValue(reader, "area_code"),
                            AccountNumber = GetColumnValue(reader, "acct_number"),
                            NetType = GetIntValue(reader, "net_type"),
                            EnergyExported = GetIntValue(reader, "units_out"),
                            EnergyImported = GetIntValue(reader, "units_in"),
                            PanelCapacity = GetDecimalValue(reader, "gen_cap"),
                            Tariff = GetColumnValue(reader, "tariff_code"),
                            BFUnits = GetIntValue(reader, "bf_units"),
                            UnitsInBill = GetIntValue(reader, "units_bill"),
                            Period = GetIntValue(reader, "period"),
                            KwhCharge = GetDecimalValue(reader, "kwh_chg"),
                            FixedCharge = GetDecimalValue(reader, "fxd_chg"),
                            FuelCharge = GetDecimalValue(reader, "fac_chg"),
                            CFUnits = GetIntValue(reader, "cf_units"),
                            Rate = GetDecimalValue(reader, "rate"),
                            UnitSale = GetIntValue(reader, "unitsale"),
                            KwhSales = GetDecimalValue(reader, "kwh_sales"),
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

        private Dictionary<string, AreaInformation> GetAreaInformationBatch(OleDbConnection conn, List<string> areaCodes)
        {
            var results = new Dictionary<string, AreaInformation>();

            if (areaCodes == null || areaCodes.Count == 0)
                return results;

            // Create IN clause for batch lookup
            var inClause = string.Join(",", areaCodes.Select(code => $"'{code.Replace("'", "''")}'"));

            string sql = $@"SELECT a.area_code, a.area_name, p.prov_name, a.region 
                           FROM areas a, provinces p 
                           WHERE a.prov_code = p.prov_code 
                           AND a.area_code IN ({inClause})";

            logger.Debug($"Area batch query SQL: {sql}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 300;

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

            return results;
        }

        private Dictionary<string, CustomerInformation> GetCustomerInformationBatch(OleDbConnection conn, string billCycle, List<string> accountNumbers)
        {
            var results = new Dictionary<string, CustomerInformation>();

            if (accountNumbers == null || accountNumbers.Count == 0)
                return results;

            // Process in batches to avoid query size limits
            const int batchSize = 500;

            for (int i = 0; i < accountNumbers.Count; i += batchSize)
            {
                var batch = accountNumbers.Skip(i).Take(batchSize).ToList();
                var batchResults = GetCustomerInformationSingleBatch(conn, billCycle, batch);

                foreach (var kvp in batchResults)
                {
                    results[kvp.Key] = kvp.Value;
                }
            }

            return results;
        }

        private Dictionary<string, CustomerInformation> GetCustomerInformationSingleBatch(OleDbConnection conn, string billCycle, List<string> accountNumbers)
        {
            var results = new Dictionary<string, CustomerInformation>();

            // Create IN clause for batch lookup
            var inClause = string.Join(",", accountNumbers.Select(acc => $"'{acc.Replace("'", "''")}'"));

            string sql = $@"SELECT acct_number, cust_fname, cust_lname, crnt_depot, substn_code 
                           FROM prn_dat_1 
                           WHERE bill_cycle = ? 
                           AND acct_number IN ({inClause})";

            logger.Debug($"Customer batch query SQL: {sql}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 300;
                cmd.Parameters.AddWithValue("@billCycle", billCycle);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var accountNumber = GetColumnValue(reader, "acct_number");
                        if (!string.IsNullOrEmpty(accountNumber))
                        {
                            string firstName = GetColumnValue(reader, "cust_fname") ?? "";
                            string lastName = GetColumnValue(reader, "cust_lname") ?? "";
                            var depot = GetColumnValue(reader, "crnt_depot") ?? string.Empty;
                            var substn = GetColumnValue(reader, "substn_code") ?? string.Empty;

                            results[accountNumber] = new CustomerInformation
                            {
                                CustomerName = $"{firstName} {lastName}".Trim(),
                                SinNumber = depot + substn
                            };
                        }
                    }
                }
            }

            return results;
        }

        private string BuildRetailQuery(RetailDetailedRequest request)
        {
            string cycleField = request.CycleType == "A" ? "bill_cycle" : "calc_cycle";
            string baseQuery = "";

            string selectColumns = @"area_code,acct_number,net_type,units_out,units_in,gen_cap,
                                    bill_cycle,tariff_code,bf_units,units_bill,period,kwh_chg,
                                    fxd_chg,fac_chg,cf_units,rate,unitsale,kwh_sales,bank_code,
                                    bran_code,bk_ac_no,agrmnt_date";

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = $@"SELECT {selectColumns} 
                                  FROM netmtcons 
                                  WHERE area_code=? AND net_type=? AND {cycleField}=? 
                                  ORDER BY acct_number";
                    break;

                case SolarReportType.Province:
                    baseQuery = $@"SELECT n.{selectColumns.Replace(",", ",n.")} 
                                  FROM netmtcons n, areas a 
                                  WHERE n.area_code = a.area_code 
                                  AND a.prov_code=? AND n.net_type=? AND n.{cycleField}=? 
                                  ORDER BY n.acct_number";
                    break;

                case SolarReportType.Region:
                    baseQuery = $@"SELECT n.{selectColumns.Replace(",", ",n.")} 
                                  FROM netmtcons n, areas a 
                                  WHERE n.area_code = a.area_code 
                                  AND a.region=? AND n.net_type=? AND n.{cycleField}=? 
                                  ORDER BY n.acct_number";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = $@"SELECT {selectColumns} 
                                  FROM netmtcons 
                                  WHERE net_type=? AND {cycleField}=? 
                                  ORDER BY acct_number";
                    break;
            }

            return baseQuery;
        }

        private void AddParameters(OleDbCommand cmd, RetailDetailedRequest request)
        {
            string cycleValue = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    cmd.Parameters.AddWithValue("@areaCode", request.AreaCode);
                    cmd.Parameters.AddWithValue("@netType", request.NetType);
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.Province:
                    cmd.Parameters.AddWithValue("@provCode", request.ProvCode);
                    cmd.Parameters.AddWithValue("@netType", request.NetType);
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.Region:
                    cmd.Parameters.AddWithValue("@region", request.Region);
                    cmd.Parameters.AddWithValue("@netType", request.NetType);
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.EntireCEB:
                default:
                    cmd.Parameters.AddWithValue("@netType", request.NetType);
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
            }
        }

        private string MapNetTypeToCustomerType(string netType)
        {
            if (string.IsNullOrEmpty(netType))
                return "Unknown";

            switch (netType.Trim())
            {
                case "1":
                    return "Net Metering";
                case "2":
                    return "Net Accounting";
                case "3":
                    return "Net Plus";
                case "4":
                    return "Net Plus Plus";
                case "5":
                    return "Convert from Net Metering to Net Accounting";
                default:
                    return "Unknown";
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
        private class RetailRawData
        {
            public string AreaCode { get; set; }
            public string AccountNumber { get; set; }
            public int NetType { get; set; }
            public int EnergyExported { get; set; }
            public int EnergyImported { get; set; }
            public decimal PanelCapacity { get; set; }
            public string Tariff { get; set; }
            public int BFUnits { get; set; }
            public int UnitsInBill { get; set; }
            public int Period { get; set; }
            public decimal KwhCharge { get; set; }
            public decimal FixedCharge { get; set; }
            public decimal FuelCharge { get; set; }
            public int CFUnits { get; set; }
            public decimal Rate { get; set; }
            public int UnitSale { get; set; }
            public decimal KwhSales { get; set; }
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
            public string SinNumber { get; set; }
        }
    }
}