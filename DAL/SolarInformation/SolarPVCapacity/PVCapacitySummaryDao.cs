using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using NLog;

namespace MISReports_Api.DAL.SolarInformation.SolarPVCapacity
{
    public class PVCapacitySummaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                logger.Debug("=== Testing Both Connections ===");

                // Test ordinary connection
                bool ordinarySuccess = _dbConnection.TestConnection(out string ordinaryError, false);
                logger.Debug($"Ordinary Connection: {(ordinarySuccess ? "OK" : ordinaryError)}");

                // Test bulk connection
                bool bulkSuccess = _dbConnection.TestConnection(out string bulkError, true);
                logger.Debug($"Bulk Connection: {(bulkSuccess ? "OK" : bulkError)}");

                if (!ordinarySuccess || !bulkSuccess)
                {
                    errorMessage = $"Ordinary: {(ordinarySuccess ? "OK" : ordinaryError)}, Bulk: {(bulkSuccess ? "OK" : bulkError)}";
                    logger.Warn(errorMessage);
                    return false;
                }

                logger.Info("Both connections tested successfully.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred during connection test");
                errorMessage = $"Connection test failed: {ex.Message}";
                return false;
            }
        }

        public List<PVCapacityModel> GetPVCapacitySummaryReport(SolarProgressRequest request)
        {
            var results = new List<PVCapacityModel>();

            try
            {
                logger.Info("=== START GetPVCapacitySummaryReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, ReportType={request.ReportType}, AreaCode={request.AreaCode}, ProvCode={request.ProvCode}, Region={request.Region}");

                // First check if bulk data has null generation capacity
                using (var bulkConn = _dbConnection.GetConnection(true))
                {
                    bulkConn.Open();

                    if (HasNullGenerationCapacity(bulkConn, request.BillCycle))
                    {
                        logger.Warn($"Null generation capacity records found for bill cycle {request.BillCycle}");
                        return results; // Return empty list if null records exist
                    }
                }

                // Get areas to process based on report type
                List<AreaInfo> areas = GetAreasToProcess(request);
                logger.Info($"Processing {areas.Count} areas");

                // Get distinct net types from bulk database
                List<string> netTypes = GetDistinctNetTypes(request.BillCycle);
                logger.Info($"Found {netTypes.Count} distinct net types");

                // Process each area and net type combination
                using (var ordinaryConn = _dbConnection.GetConnection(false))
                using (var bulkConn = _dbConnection.GetConnection(true))
                {
                    ordinaryConn.Open();
                    bulkConn.Open();

                    foreach (var area in areas)
                    {
                        foreach (var netType in netTypes)
                        {
                            var summary = GetCombinedSummary(
                                ordinaryConn,
                                bulkConn,
                                request.BillCycle,
                                area,
                                netType);

                            if (summary != null && (summary.NoOfConsumers > 0 || summary.Capacity > 0))
                            {
                                results.Add(summary);
                            }
                        }
                    }
                }

                logger.Info("=== END GetPVCapacitySummaryReport (Success) ===");
                logger.Info($"Returning {results.Count} results");
                return results;
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "Database error occurred while fetching PV capacity summary report");
                throw new ApplicationException("Database error occurred while fetching report", ex);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error occurred while fetching PV capacity summary report");
                throw;
            }
        }

        private bool HasNullGenerationCapacity(OleDbConnection bulkConn, string billCycle)
        {
            try
            {
                string nullCheckSql = "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND gen_cap IS NULL";
                logger.Debug($"Checking for null generation capacity records: {nullCheckSql}");

                using (var cmd = new OleDbCommand(nullCheckSql, bulkConn))
                {
                    cmd.Parameters.AddWithValue("", billCycle);

                    var result = cmd.ExecuteScalar();
                    int nullCount = result != null ? Convert.ToInt32(result) : 0;

                    logger.Info($"Null generation capacity records count: {nullCount}");

                    return nullCount > 0;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error checking for null generation capacity records");
                throw;
            }
        }

        private List<AreaInfo> GetAreasToProcess(SolarProgressRequest request)
        {
            var areas = new List<AreaInfo>();

            try
            {
                using (var conn = _dbConnection.GetConnection(false)) // Use ordinary connection for areas table
                {
                    conn.Open();

                    string sql = BuildAreaQuery(request);
                    logger.Debug($"Area query: {sql}");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        AddAreaQueryParameters(cmd, request);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                areas.Add(new AreaInfo
                                {
                                    AreaCode = GetColumnValue(reader, "area_code"),
                                    AreaName = GetColumnValue(reader, "area_name"),
                                    Region = GetColumnValue(reader, "region"),
                                    ProvCode = GetColumnValue(reader, "prov_code")
                                });
                            }
                        }
                    }

                    // Get province names
                    if (areas.Count > 0)
                    {
                        var provinceNames = GetProvinceNames(conn, areas);
                        foreach (var area in areas)
                        {
                            if (provinceNames.TryGetValue(area.ProvCode, out string provName))
                            {
                                area.ProvinceName = provName;
                            }
                        }
                    }
                }

                logger.Info($"Loaded {areas.Count} areas to process");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading areas to process");
                throw;
            }

            return areas;
        }

        private string BuildAreaQuery(SolarProgressRequest request)
        {
            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    return "SELECT * FROM areas WHERE area_code = ?";

                case SolarReportType.Province:
                    return "SELECT * FROM areas WHERE prov_code = ?";

                case SolarReportType.Region:
                    return "SELECT * FROM areas WHERE region = ? ORDER BY prov_code, area_name";

                case SolarReportType.EntireCEB:
                default:
                    return "SELECT * FROM areas ORDER BY region, prov_code, area_name";
            }
        }

        private void AddAreaQueryParameters(OleDbCommand cmd, SolarProgressRequest request)
        {
            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    logger.Debug("Adding parameter: AreaCode={0}", request.AreaCode);
                    cmd.Parameters.AddWithValue("", request.AreaCode);
                    break;

                case SolarReportType.Province:
                    logger.Debug("Adding parameter: ProvCode={0}", request.ProvCode);
                    cmd.Parameters.AddWithValue("", request.ProvCode);
                    break;

                case SolarReportType.Region:
                    logger.Debug("Adding parameter: Region={0}", request.Region);
                    cmd.Parameters.AddWithValue("", request.Region);
                    break;

                case SolarReportType.EntireCEB:
                    // No parameters needed
                    break;
            }
        }

        private Dictionary<string, string> GetProvinceNames(OleDbConnection conn, List<AreaInfo> areas)
        {
            var provinceNames = new Dictionary<string, string>();

            try
            {
                var uniqueProvCodes = areas.Select(a => a.ProvCode).Distinct().ToList();

                if (uniqueProvCodes.Count == 0)
                    return provinceNames;

                string sql = "SELECT prov_code, prov_name FROM provinces";
                logger.Debug($"Loading province names: {sql}");

                using (var cmd = new OleDbCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string provCode = GetColumnValue(reader, "prov_code");
                        string provName = GetColumnValue(reader, "prov_name");

                        if (!string.IsNullOrEmpty(provCode) && !provinceNames.ContainsKey(provCode))
                        {
                            provinceNames[provCode] = provName;
                        }
                    }
                }

                logger.Debug($"Loaded {provinceNames.Count} province names");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading province names");
            }

            return provinceNames;
        }

        private List<string> GetDistinctNetTypes(string billCycle)
        {
            var netTypes = new List<string>();

            try
            {
                using (var conn = _dbConnection.GetConnection(true)) // Use bulk connection
                {
                    conn.Open();

                    string sql = "SELECT DISTINCT net_type FROM netmtcons WHERE bill_cycle = ? ORDER BY net_type";
                    logger.Debug($"Getting distinct net types: {sql}");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("", billCycle);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = GetColumnValue(reader, "net_type");
                                if (!string.IsNullOrEmpty(netType))
                                {
                                    netTypes.Add(netType);
                                }
                            }
                        }
                    }
                }

                logger.Info($"Found {netTypes.Count} distinct net types: {string.Join(", ", netTypes)}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error getting distinct net types");
                throw;
            }

            return netTypes;
        }

        private PVCapacityModel GetCombinedSummary(
            OleDbConnection ordinaryConn,
            OleDbConnection bulkConn,
            string billCycle,
            AreaInfo area,
            string netType)
        {
            try
            {
                // Get ordinary data
                var ordinaryData = GetOrdinaryData(ordinaryConn, billCycle, area.AreaCode, netType);

                // Get bulk data
                var bulkData = GetBulkData(bulkConn, billCycle, area.AreaCode, netType);

                // Combine the data
                var summary = new PVCapacityModel
                {
                    NetType = MapNetType(netType),
                    Division = area.Region,
                    Province = area.ProvinceName,
                    Area = area.AreaName,
                    NoOfConsumers = ordinaryData.Consumers + bulkData.Consumers,
                    Capacity = ordinaryData.Capacity + bulkData.Capacity
                };

                logger.Trace($"Combined summary: Area={area.AreaName}, NetType={netType}, " +
                           $"Ordinary(Consumers={ordinaryData.Consumers}, Cap={ordinaryData.Capacity}), " +
                           $"Bulk(Consumers={bulkData.Consumers}, Cap={bulkData.Capacity}), " +
                           $"Total(Consumers={summary.NoOfConsumers}, Cap={summary.Capacity})");

                return summary;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting combined summary for area {area.AreaCode}, net type {netType}");
                throw;
            }
        }

        private (int Consumers, decimal Capacity) GetOrdinaryData(
            OleDbConnection conn,
            string billCycle,
            string areaCode,
            string netType)
        {
            try
            {
                string sql = "SELECT SUM(cnt) AS total_cnt, SUM(tot_gen_cap) AS total_cap " +
                           "FROM netprogrs " +
                           "WHERE bill_cycle = ? AND area_code = ? AND net_type = ? " +
                           "GROUP BY bill_cycle, area_code";

                logger.Trace($"Ordinary query: {sql} [billCycle={billCycle}, areaCode={areaCode}, netType={netType}]");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("", billCycle);
                    cmd.Parameters.AddWithValue("", areaCode);
                    cmd.Parameters.AddWithValue("", netType);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int consumers = GetIntValue(reader, "total_cnt");
                            decimal capacity = GetDecimalValue(reader, "total_cap");

                            return (consumers, capacity);
                        }
                    }
                }

                return (0, 0);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting ordinary data for area {areaCode}, net type {netType}");
                return (0, 0);
            }
        }

        private (int Consumers, decimal Capacity) GetBulkData(
            OleDbConnection conn,
            string billCycle,
            string areaCode,
            string netType)
        {
            try
            {
                string sql = "SELECT COUNT(*) AS total_cnt, SUM(gen_cap) AS total_cap " +
                           "FROM netmtcons " +
                           "WHERE bill_cycle = ? AND area_cd = ? AND net_type = ?";

                logger.Trace($"Bulk query: {sql} [billCycle={billCycle}, areaCode={areaCode}, netType={netType}]");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("", billCycle);
                    cmd.Parameters.AddWithValue("", areaCode);
                    cmd.Parameters.AddWithValue("", netType);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int consumers = GetIntValue(reader, "total_cnt");
                            decimal capacity = GetDecimalValue(reader, "total_cap");

                            return (consumers, capacity);
                        }
                    }
                }

                return (0, 0);
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting bulk data for area {areaCode}, net type {netType}");
                return (0, 0);
            }
        }

        // Helper methods
        private string GetColumnValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? null : value.ToString().Trim();
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
                logger.Warn(ex, $"Invalid integer format in column '{columnName}'");
                return 0;
            }
        }

        private string MapNetType(string netTypeCode)
        {
            if (string.IsNullOrEmpty(netTypeCode))
                return "Unknown";

            string trimmedCode = netTypeCode.Trim();

            switch (trimmedCode)
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
                    return $"Unknown ({trimmedCode})";
            }
        }

        // Helper class for area information
        private class AreaInfo
        {
            public string AreaCode { get; set; }
            public string AreaName { get; set; }
            public string Region { get; set; }
            public string ProvCode { get; set; }
            public string ProvinceName { get; set; }
        }
    }
}