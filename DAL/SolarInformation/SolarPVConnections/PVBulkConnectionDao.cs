using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.SolarInformation.SolarPVConnections
{
    public class PVBulkConnectionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true);
        }

        public List<SolarPVBulkConnectionModel> GetPVConnections(SolarPVBulkConnectionRequest request)
        {
            var results = new List<SolarPVBulkConnectionModel>();

            try
            {
                logger.Info("=== START GetPVConnections ===");
                logger.Info($"Request: {request.CycleType}Cycle={request.BillCycle ?? request.CalcCycle}, ReportType={request.ReportType}");

                string sql = BuildPVBulkConnectionQuery(request);
                logger.Debug($"Generated SQL: {sql}");

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // Set command timeout for large queries
                        cmd.CommandTimeout = 300; // 5 minutes

                        AddParameters(cmd, request);

                        using (var reader = cmd.ExecuteReader())
                        {
                            // First, collect all records and account numbers
                            var tempResults = new List<SolarPVBulkConnectionModel>();
                            var accountNumbers = new List<string>();

                            while (reader.Read())
                            {
                                var pvConnection = MapPVConnectionFromReader(reader);
                                tempResults.Add(pvConnection);
                                accountNumbers.Add(pvConnection.AccountNumber);
                            }

                            // Bulk fetch all additional data
                            results = BulkEnrichPVConnectionData(accountNumbers, tempResults);
                        }
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

        private string BuildPVBulkConnectionQuery(SolarPVBulkConnectionRequest request)
        {
            // The cycle value is always bill_cycle regardless of CycleType (as per your SQL examples)
            string cycleValue = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;

            string baseQuery = "";

            // Build query based on report type
            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = @"
                        SELECT * 
                        FROM netmtcons n, customer c, inst_info i 
                        WHERE n.acc_nbr = c.acc_nbr 
                        AND c.inst_id = i.inst_id 
                        AND n.bill_cycle = ? 
                        AND n.area_cd = ? 
                        ORDER BY n.acc_nbr";
                    break;

                case SolarReportType.Province:
                    baseQuery = @"
                        SELECT * 
                        FROM netmtcons n, customer c, inst_info i, areas a 
                        WHERE n.acc_nbr = c.acc_nbr 
                        AND c.inst_id = i.inst_id 
                        AND n.bill_cycle = ? 
                        AND a.prov_code = ? 
                        AND a.area_code = n.area_cd 
                        ORDER BY n.acc_nbr";
                    break;

                case SolarReportType.Region:
                    baseQuery = @"
                        SELECT * 
                        FROM netmtcons n, customer c, inst_info i, areas a 
                        WHERE n.acc_nbr = c.acc_nbr 
                        AND c.inst_id = i.inst_id 
                        AND n.bill_cycle = ? 
                        AND a.region = ? 
                        AND a.area_code = n.area_cd 
                        ORDER BY n.acc_nbr";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = @"
                        SELECT * 
                        FROM netmtcons n, customer c, inst_info i, areas a 
                        WHERE n.acc_nbr = c.acc_nbr 
                        AND c.inst_id = i.inst_id 
                        AND n.bill_cycle = ? 
                        AND a.area_code = n.area_cd 
                        ORDER BY n.acc_nbr";
                    break;
            }

            return baseQuery;
        }

        private void AddParameters(OleDbCommand cmd, SolarPVBulkConnectionRequest request)
        {
            string cycleValue = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;

            // Add cycle parameter (always first parameter)
            cmd.Parameters.AddWithValue("@cycle", cycleValue);

            // Add specific filters based on report type
            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    if (!string.IsNullOrEmpty(request.AreaCode))
                    {
                        cmd.Parameters.AddWithValue("@areaCode", request.AreaCode);
                    }
                    break;
                case SolarReportType.Province:
                    if (!string.IsNullOrEmpty(request.ProvCode))
                    {
                        cmd.Parameters.AddWithValue("@provCode", request.ProvCode);
                    }
                    break;
                case SolarReportType.Region:
                    if (!string.IsNullOrEmpty(request.Region))
                    {
                        cmd.Parameters.AddWithValue("@region", request.Region);
                    }
                    break;
                case SolarReportType.EntireCEB:
                    // No additional parameters needed
                    break;
            }
        }

        private List<SolarPVBulkConnectionModel> BulkEnrichPVConnectionData(List<string> accountNumbers, List<SolarPVBulkConnectionModel> models)
        {
            try
            {
                if (!accountNumbers.Any())
                    return models;

                // Bulk get agreement dates
                var agreementDates = BulkGetAgreementDates(accountNumbers);

                // Bulk get province/region data
                var areaData = BulkGetAreaData(accountNumbers);

                // Enrich all models in memory
                foreach (var model in models)
                {
                    if (agreementDates.TryGetValue(model.AccountNumber, out var agreementDate))
                    {
                        model.AgreementDate = agreementDate;
                    }

                    if (areaData.TryGetValue(model.AccountNumber, out var areaInfo))
                    {
                        if (string.IsNullOrEmpty(model.Province)) model.Province = areaInfo.Province;
                        if (string.IsNullOrEmpty(model.Division)) model.Division = areaInfo.Division;
                        if (string.IsNullOrEmpty(model.Area)) model.Area = areaInfo.AreaName;
                    }
                }

                return models;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in bulk data enrichment");
                return models; // Return original data if enrichment fails
            }
        }

        private Dictionary<string, string> BulkGetAgreementDates(List<string> accountNumbers)
        {
            var results = new Dictionary<string, string>();

            if (!accountNumbers.Any())
                return results;

            try
            {
                // Create parameter placeholders for IN clause
                var parameters = accountNumbers.Select((_, index) => "?").ToArray();
                string sql = $"SELECT acc_nbr, agrmnt_date FROM netmeter WHERE acc_nbr IN ({string.Join(",", parameters)})";

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // Add all account numbers as parameters
                        for (int i = 0; i < accountNumbers.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@accNbr{i}", accountNumbers[i]);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var accNbr = GetColumnValue(reader, "acc_nbr");
                                var agreementDateStr = GetColumnValue(reader, "agrmnt_date");

                                if (!string.IsNullOrEmpty(accNbr))
                                {
                                    if (DateTime.TryParse(agreementDateStr, out var agreementDate))
                                    {
                                        results[accNbr] = agreementDate.ToString("yyyy-MM-dd");
                                    }
                                    else
                                    {
                                        results[accNbr] = agreementDateStr;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in bulk agreement dates fetch");
            }

            return results;
        }

        private Dictionary<string, (string Province, string Division, string AreaName)> BulkGetAreaData(List<string> accountNumbers)
        {
            var results = new Dictionary<string, (string, string, string)>();

            if (!accountNumbers.Any())
                return results;

            try
            {
                // First get area codes for all accounts
                var accountAreaCodes = BulkGetAreaCodesForAccounts(accountNumbers);

                if (!accountAreaCodes.Any())
                    return results;

                // Then get area details for all unique area codes
                var uniqueAreaCodes = accountAreaCodes.Values.Distinct().Where(ac => !string.IsNullOrEmpty(ac)).ToList();

                if (!uniqueAreaCodes.Any())
                    return results;

                var areaDetails = BulkGetAreaDetails(uniqueAreaCodes);

                // Map back to accounts
                foreach (var account in accountAreaCodes)
                {
                    if (areaDetails.TryGetValue(account.Value, out var details))
                    {
                        results[account.Key] = details;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in bulk area data fetch");
            }

            return results;
        }

        private Dictionary<string, string> BulkGetAreaCodesForAccounts(List<string> accountNumbers)
        {
            var results = new Dictionary<string, string>();

            try
            {
                var parameters = accountNumbers.Select((_, index) => "?").ToArray();
                string sql = $"SELECT acc_nbr, area_cd FROM netmtcons WHERE acc_nbr IN ({string.Join(",", parameters)})";

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        for (int i = 0; i < accountNumbers.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@accNbr{i}", accountNumbers[i]);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var accNbr = GetColumnValue(reader, "acc_nbr");
                                var areaCode = GetColumnValue(reader, "area_cd");

                                if (!string.IsNullOrEmpty(accNbr))
                                {
                                    results[accNbr] = areaCode;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in bulk area codes fetch");
            }

            return results;
        }

        private Dictionary<string, (string Province, string Division, string AreaName)> BulkGetAreaDetails(List<string> areaCodes)
        {
            var results = new Dictionary<string, (string, string, string)>();

            try
            {
                var parameters = areaCodes.Select((_, index) => "?").ToArray();
                string sql = $@"
                    SELECT a.area_code, p.prov_name, a.region, a.area_name 
                    FROM areas a 
                    LEFT JOIN provinces p ON a.prov_code = p.prov_code 
                    WHERE a.area_code IN ({string.Join(",", parameters)})";

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        for (int i = 0; i < areaCodes.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@areaCode{i}", areaCodes[i]);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var areaCode = GetColumnValue(reader, "area_code");
                                var provName = GetColumnValue(reader, "prov_name");
                                var region = GetColumnValue(reader, "region");
                                var areaName = GetColumnValue(reader, "area_name");

                                if (!string.IsNullOrEmpty(areaCode))
                                {
                                    results[areaCode] = (provName, region, areaName);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in bulk area details fetch");
            }

            return results;
        }

        private SolarPVBulkConnectionModel MapPVConnectionFromReader(OleDbDataReader reader)
        {
            var model = new SolarPVBulkConnectionModel();

            try
            {
                model.AccountNumber = GetColumnValue(reader, "acc_nbr");

                // Try to get area name, fallback to area code if not available
                model.Area = GetColumnValue(reader, "area_name");

                // Province and Division might be available depending on the query
                model.Province = GetColumnValue(reader, "prov_name");
                model.Division = GetColumnValue(reader, "region");

                model.PanelCapacity = GetDecimalValue(reader, "gen_cap");
                model.EnergyExported = GetIntValue(reader, "exp_kwd_units");
                model.EnergyImported =
    GetIntValue(reader, "imp_kwo_units") +
    GetIntValue(reader, "imp_kwd_units") +
    GetIntValue(reader, "imp_kwp_units");

                model.Tariff = GetColumnValue(reader, "tariff");

                // Customer name from customer table
                model.CustomerName = GetColumnValue(reader, "name");

                // Customer type from net_type
                string netType = GetColumnValue(reader, "net_type");
                model.CustomerType = MapNetTypeToCustomerType(netType);

                // B/F and C/F units
                model.BFUnits = GetIntValue(reader, "bf_units");
                model.CFUnits = GetIntValue(reader, "cf_units");

                // SIN Number construction
                var depot = GetColumnValue(reader, "dp_code") ?? string.Empty;
                var substn = GetColumnValue(reader, "cnnct_trpnl") ?? string.Empty;
                model.SinNumber = depot + substn;

                model.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error mapping PV connection data for account {model.AccountNumber}");
                model.ErrorMessage = ex.Message;
            }

            return model;
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
    }
}