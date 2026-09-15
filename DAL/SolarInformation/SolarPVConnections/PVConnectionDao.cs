// Optimized PVConnectionDao.cs - High Performance Version
using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.SolarInformation.SolarPVConnections
{
    public class PVConnectionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        public List<SolarPVConnectionModel> GetPVConnections(SolarPVConnectionRequest request)
        {
            var results = new List<SolarPVConnectionModel>();

            try
            {
                logger.Info("=== START GetPVConnections (Optimized) ===");
                logger.Info($"Request: {request.CycleType}Cycle={request.BillCycle ?? request.CalcCycle}, ReportType={request.ReportType}");

                // Get bill cycle for customer lookups
                string billCycleForCustomer = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Step 1: Get main PV connection data
                    var pvData = GetPVConnectionData(conn, request);
                    logger.Info($"Retrieved {pvData.Count} PV connection records");

                    if (pvData.Count == 0)
                    {
                        logger.Info("No PV connection data found");
                        return results;
                    }

                    // Step 2: Get area information (province, region) in batch
                    var areaInfo = GetAreaInformationBatch(conn, pvData.Select(p => p.AreaCode).Distinct().ToList());
                    logger.Info($"Retrieved area information for {areaInfo.Count} areas");

                    // Step 3: Get customer information in batch
                    var customerInfo = GetCustomerInformationBatch(conn, billCycleForCustomer, pvData.Select(p => p.AccountNumber).ToList());
                    logger.Info($"Retrieved customer information for {customerInfo.Count} accounts");

                    // Step 4: Combine all data
                    foreach (var pvRecord in pvData)
                    {
                        var model = new SolarPVConnectionModel
                        {
                            AccountNumber = pvRecord.AccountNumber,
                            PanelCapacity = pvRecord.PanelCapacity,
                            EnergyExported = pvRecord.EnergyExported,
                            EnergyImported = pvRecord.EnergyImported,
                            Tariff = pvRecord.Tariff,
                            BFUnits = pvRecord.BFUnits,
                            CFUnits = pvRecord.CFUnits,
                            AgreementDate = pvRecord.AgreementDate,
                            CustomerType = MapNetTypeToCustomerType(pvRecord.NetType.ToString()),
                            UnitsForLossReduction = CalculateUnitsForLossReduction(
                                pvRecord.NetType,
                                pvRecord.BFUnits,
                                pvRecord.EnergyImported,
                                pvRecord.EnergyExported)
                        };

                        // Add area information
                        if (areaInfo.TryGetValue(pvRecord.AreaCode, out var area))
                        {
                            model.Area = area.AreaName;
                            model.Province = area.ProvinceName;
                            model.Division = area.Region;
                        }
                        else
                        {
                            model.Area = pvRecord.AreaCode;
                        }

                        // Add customer information
                        if (customerInfo.TryGetValue(pvRecord.AccountNumber, out var customer))
                        {
                            model.CustomerName = customer.CustomerName;
                            model.SinNumber = customer.SinNumber;
                        }
                        else
                        {
                            model.CustomerName = "Unknown";
                            model.SinNumber = "";
                        }

                        model.ErrorMessage = string.Empty;
                        results.Add(model);
                    }
                }

                logger.Info($"=== END GetPVConnections (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching PV connections");
                throw;
            }
        }

        private List<PVConnectionRawData> GetPVConnectionData(OleDbConnection conn, SolarPVConnectionRequest request)
        {
            var results = new List<PVConnectionRawData>();
            string sql = BuildPVConnectionQuery(request);

            logger.Debug($"Main query SQL: {sql}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 300; // 5 minutes
                AddParameters(cmd, request);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = new PVConnectionRawData
                        {
                            AreaCode = GetColumnValue(reader, "area_code"),
                            AccountNumber = GetColumnValue(reader, "acct_number"),
                            NetType = GetIntValue(reader, "net_type"),
                            BFUnits = GetIntValue(reader, "bf_units"),
                            EnergyExported = GetIntValue(reader, "units_out"),
                            EnergyImported = GetIntValue(reader, "units_in"),
                            CFUnits = GetIntValue(reader, "cf_units"),
                            PanelCapacity = GetDecimalValue(reader, "gen_cap"),
                            Tariff = GetColumnValue(reader, "tariff_code")
                        };

                        var agreementDateStr = GetColumnValue(reader, "agrmnt_date");
                        if (DateTime.TryParse(agreementDateStr, out var agreementDate))
                        {
                            data.AgreementDate = agreementDate.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            data.AgreementDate = agreementDateStr;
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
            const int batchSize = 500; // Adjust based on your database limits

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

        private string BuildPVConnectionQuery(SolarPVConnectionRequest request)
        {
            string cycleField = request.CycleType == "A" ? "bill_cycle" : "calc_cycle";
            string baseQuery = "";

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = $@"SELECT area_code,acct_number,net_type,bf_units,units_out,units_in,cf_units,gen_cap,tariff_code,agrmnt_date 
                                  FROM netmtcons 
                                  WHERE area_code=? and {cycleField} = ? 
                                  ORDER BY acct_number";
                    break;

                case SolarReportType.Province:
                    baseQuery = $@"SELECT n.area_code,n.acct_number,n.net_type,n.bf_units,n.units_out,n.units_in,n.cf_units,n.gen_cap,n.tariff_code,n.agrmnt_date 
                                  FROM netmtcons n, areas a 
                                  WHERE n.area_code = a.area_code 
                                  AND a.prov_code = ? 
                                  AND n.{cycleField} = ? 
                                  ORDER BY n.acct_number";
                    break;

                case SolarReportType.Region:
                    baseQuery = $@"SELECT n.area_code,n.acct_number,n.net_type,n.bf_units,n.units_out,n.units_in,n.cf_units,n.gen_cap,n.tariff_code,n.agrmnt_date 
                                  FROM netmtcons n, areas a 
                                  WHERE n.area_code = a.area_code 
                                  AND a.region = ? 
                                  AND n.{cycleField} = ? 
                                  ORDER BY n.acct_number";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = $@"SELECT area_code,acct_number,net_type,bf_units,units_out,units_in,cf_units,gen_cap,tariff_code,agrmnt_date 
                                  FROM netmtcons 
                                  WHERE {cycleField} = ? 
                                  ORDER BY acct_number";
                    break;
            }

            return baseQuery;
        }

        private void AddParameters(OleDbCommand cmd, SolarPVConnectionRequest request)
        {
            string cycleValue = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    cmd.Parameters.AddWithValue("@areaCode", request.AreaCode);
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.Province:
                    cmd.Parameters.AddWithValue("@provCode", request.ProvCode);
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.Region:
                    cmd.Parameters.AddWithValue("@region", request.Region);
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
                case SolarReportType.EntireCEB:
                default:
                    cmd.Parameters.AddWithValue("@cycle", cycleValue);
                    break;
            }
        }

        private int CalculateUnitsForLossReduction(int netType, int bfUnits, int unitsIn, int unitsOut)
        {
            int lossReduc = 0;

            try
            {
                if (netType == 1)
                {
                    if (bfUnits > 0 && unitsIn > unitsOut)
                    {
                        lossReduc = bfUnits;
                    }
                    else if (unitsOut > unitsIn)
                    {
                        lossReduc = unitsOut - unitsIn;
                    }
                }
                else if (netType == 2)
                {
                    if (unitsIn > unitsOut)
                    {
                        lossReduc = 0;
                    }
                    else if (unitsOut > unitsIn)
                    {
                        lossReduc = unitsOut - unitsIn;
                    }
                }
                else if (netType == 3)
                {
                    lossReduc = 0;
                }

                logger.Debug($"Loss reduction calculation: NetType={netType}, BFUnits={bfUnits}, UnitsIn={unitsIn}, UnitsOut={unitsOut}, Result={lossReduc}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error calculating loss reduction for NetType={netType}");
                lossReduc = 0;
            }

            return lossReduc;
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
        private class PVConnectionRawData
        {
            public string AreaCode { get; set; }
            public string AccountNumber { get; set; }
            public int NetType { get; set; }
            public int BFUnits { get; set; }
            public int EnergyExported { get; set; }
            public int EnergyImported { get; set; }
            public int CFUnits { get; set; }
            public decimal PanelCapacity { get; set; }
            public string Tariff { get; set; }
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