using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using NLog;

namespace MISReports_Api.DAL.SolarInformation.SolarPVCapacity
{
    public class PVCapacityBulkDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                logger.Debug("=== Testing Bulk Connection ===");
                logger.Debug($"Connection String: {_dbConnection.BulkConnectionString}");

                var builder = new OleDbConnectionStringBuilder(_dbConnection.BulkConnectionString);
                logger.Debug($"Data Source: {builder.DataSource}");
                logger.Debug($"Provider: {builder.Provider}");

                if (string.IsNullOrEmpty(builder.DataSource))
                {
                    errorMessage = "Data Source is missing from connection string";
                    logger.Warn(errorMessage);
                    return false;
                }

                if (string.IsNullOrEmpty(builder.Provider))
                {
                    errorMessage = "Provider is missing from connection string";
                    logger.Warn(errorMessage);
                    return false;
                }

                using (var conn = _dbConnection.GetConnection(true)) // Use bulk connection
                {
                    logger.Debug("Attempting to open bulk connection...");
                    conn.Open();
                    logger.Info("Bulk database connection opened successfully.");
                    return true;
                }
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "OLE DB Error occurred during bulk connection test");

                if (ex.Errors != null && ex.Errors.Count > 0)
                {
                    for (int i = 0; i < ex.Errors.Count; i++)
                    {
                        logger.Error($"Error {i}: SQLState={ex.Errors[i].SQLState}, NativeError={ex.Errors[i].NativeError}");
                    }
                }

                errorMessage = $"Database connection failed: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "General error occurred during bulk connection test");
                errorMessage = $"Connection test failed: {ex.Message}";
                return false;
            }
        }

        public List<PVCapacityModel> GetPVCapacityBulkReport(SolarProgressRequest request)
        {
            var results = new List<PVCapacityModel>();

            try
            {
                logger.Info("=== START GetPVCapacityBulkReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, ReportType={request.ReportType}, AreaCode={request.AreaCode}, ProvCode={request.ProvCode}, Region={request.Region}");

                using (var conn = _dbConnection.GetConnection(true)) // Use bulk connection
                {
                    logger.Debug("Opening bulk database connection...");
                    conn.Open();
                    logger.Debug("Bulk database connection opened successfully.");

                    // Check for null generation capacity records
                    if (HasNullGenerationCapacity(conn, request.BillCycle))
                    {
                        logger.Warn($"Null generation capacity records found for bill cycle {request.BillCycle}");
                        return results; // Return empty list if null records exist
                    }

                    logger.Debug("Loading area metadata mapping...");
                    var areaMetadata = GetAreaMetadata(conn);
                    logger.Info($"Loaded {areaMetadata.Count} area metadata entries");

                    string sql = BuildSqlQuery(request);
                    logger.Debug($"Generated SQL: {sql}");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        AddQueryParameters(cmd, request);

                        logger.Debug("Executing query...");
                        using (var reader = cmd.ExecuteReader())
                        {
                            logger.Debug("Reading results...");
                            int recordCount = 0;

                            while (reader.Read())
                            {
                                recordCount++;
                                var pvCapacity = MapPVCapacityFromReader(reader, areaMetadata);
                                results.Add(pvCapacity);
                            }

                            logger.Info($"Successfully read {recordCount} records from database");
                        }
                    }
                }

                logger.Info("=== END GetPVCapacityBulkReport (Success) ===");
                logger.Info($"Returning {results.Count} results");
                return results;
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "Database error occurred while fetching PV capacity bulk report");
                logger.Error($"SQL: {BuildSqlQuery(request)}");
                throw new ApplicationException("Database error occurred while fetching report", ex);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error occurred while fetching PV capacity bulk report");
                throw;
            }
        }

        private bool HasNullGenerationCapacity(OleDbConnection conn, string billCycle)
        {
            try
            {
                string nullCheckSql = "SELECT COUNT(*) FROM netmtcons WHERE bill_cycle = ? AND gen_cap IS NULL";
                logger.Debug($"Checking for null generation capacity records: {nullCheckSql}");

                using (var cmd = new OleDbCommand(nullCheckSql, conn))
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

        private string BuildSqlQuery(SolarProgressRequest request)
        {
            string baseQuery;

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = "SELECT area_cd, bill_cycle, net_type, COUNT(*) AS cnt, SUM(gen_cap) AS tot_gen_cap " +
                               "FROM netmtcons " +
                               "WHERE bill_cycle = ? AND area_cd = ? " +
                               "GROUP BY bill_cycle, net_type, area_cd " +
                               "ORDER BY bill_cycle, net_type, area_cd";
                    break;

                case SolarReportType.Province:
                    // Check if province code is numeric
                    if (IsNumericProvinceCode(request.ProvCode))
                    {
                        // Prefix with '0' for numeric province codes
                        baseQuery = "SELECT n.area_cd, n.bill_cycle, n.net_type, COUNT(*) AS cnt, SUM(n.gen_cap) AS tot_gen_cap " +
                                   "FROM netmtcons n, areas a " +
                                   "WHERE n.area_cd = a.area_code AND a.prov_code = ? AND n.bill_cycle = ? " +
                                   "GROUP BY n.area_cd, n.bill_cycle, n.net_type " +
                                   "ORDER BY n.bill_cycle, n.area_cd, n.net_type";
                    }
                    else
                    {
                        baseQuery = "SELECT n.area_cd, n.bill_cycle, n.net_type, COUNT(*) AS cnt, SUM(n.gen_cap) AS tot_gen_cap " +
                                   "FROM netmtcons n, areas a " +
                                   "WHERE n.area_cd = a.area_code AND a.prov_code = ? AND n.bill_cycle = ? " +
                                   "GROUP BY n.area_cd, n.bill_cycle, n.net_type " +
                                   "ORDER BY n.bill_cycle, n.area_cd, n.net_type";
                    }
                    break;

                case SolarReportType.Region:
                    baseQuery = "SELECT n.area_cd, n.bill_cycle, n.net_type, COUNT(*) AS cnt, SUM(n.gen_cap) AS tot_gen_cap " +
                               "FROM netmtcons n, areas a " +
                               "WHERE n.area_cd = a.area_code AND a.region = ? AND n.bill_cycle = ? " +
                               "GROUP BY n.area_cd, n.bill_cycle, n.net_type " +
                               "ORDER BY n.bill_cycle, n.area_cd, n.net_type";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = "SELECT area_cd, bill_cycle, net_type, COUNT(*) AS cnt, SUM(gen_cap) AS tot_gen_cap " +
                               "FROM netmtcons " +
                               "WHERE bill_cycle = ? " +
                               "GROUP BY area_cd, bill_cycle, net_type " +
                               "ORDER BY bill_cycle, area_cd, net_type";
                    break;
            }

            return baseQuery;
        }

        private void AddQueryParameters(OleDbCommand cmd, SolarProgressRequest request)
        {
            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    logger.Debug("Adding parameters: BillCycle={0}, AreaCode={1}", request.BillCycle, request.AreaCode);
                    cmd.Parameters.AddWithValue("", request.BillCycle);
                    cmd.Parameters.AddWithValue("", request.AreaCode);
                    break;

                case SolarReportType.Province:
                    string provCode = request.ProvCode;

                    // Add '0' prefix for numeric province codes
                    if (IsNumericProvinceCode(provCode))
                    {
                        provCode = "0" + provCode;
                        logger.Debug("Numeric province code detected, adding '0' prefix: {0}", provCode);
                    }

                    logger.Debug("Adding parameters: ProvCode={0}, BillCycle={1}", provCode, request.BillCycle);
                    cmd.Parameters.AddWithValue("", provCode);
                    cmd.Parameters.AddWithValue("", request.BillCycle);
                    break;

                case SolarReportType.Region:
                    logger.Debug("Adding parameters: Region={0}, BillCycle={1}", request.Region, request.BillCycle);
                    cmd.Parameters.AddWithValue("", request.Region);
                    cmd.Parameters.AddWithValue("", request.BillCycle);
                    break;

                case SolarReportType.EntireCEB:
                    logger.Debug("Adding parameter: BillCycle={0}", request.BillCycle);
                    cmd.Parameters.AddWithValue("", request.BillCycle);
                    break;
            }
        }

        private bool IsNumericProvinceCode(string provCode)
        {
            if (string.IsNullOrEmpty(provCode) || provCode.Length == 0)
                return false;

            // Check if the first character is a digit (ASCII 48-57)
            char firstChar = provCode[0];
            bool isNumeric = firstChar >= '0' && firstChar <= '9';

            logger.Debug($"Province code '{provCode}' first char ASCII: {(int)firstChar}, IsNumeric: {isNumeric}");

            return isNumeric;
        }

        private PVCapacityModel MapPVCapacityFromReader(OleDbDataReader reader, Dictionary<string, AreaMetadata> areaMetadata)
        {
            try
            {
                if (logger.IsDebugEnabled)
                {
                    var availableColumns = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        availableColumns.Add(reader.GetName(i));
                    }
                    logger.Debug($"Available columns: {string.Join(", ", availableColumns)}");
                }

                string areaCode = GetColumnValue(reader, "area_cd");
                string netType = GetColumnValue(reader, "net_type");

                var model = new PVCapacityModel
                {
                    NetType = MapNetType(netType),
                    Division = string.Empty,
                    Province = string.Empty,
                    Area = string.Empty,
                    NoOfConsumers = GetIntValue(reader, "cnt"),
                    Capacity = GetDecimalValue(reader, "tot_gen_cap")
                };

                // Populate area metadata
                if (areaMetadata.TryGetValue(areaCode, out AreaMetadata metadata))
                {
                    model.Division = metadata.Region;
                    model.Province = metadata.ProvinceName;
                    model.Area = metadata.AreaName;
                }
                else
                {
                    logger.Warn($"Area code '{areaCode}' not found in metadata dictionary");
                    model.Area = $"{areaCode} (Unknown Area)";
                }

                logger.Trace($"Mapped record: Area={model.Area}, NetType={model.NetType}, Consumers={model.NoOfConsumers}, Capacity={model.Capacity}");

                return model;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping data from database reader");
                throw;
            }
        }

        private Dictionary<string, AreaMetadata> GetAreaMetadata(OleDbConnection conn)
        {
            var areaMetadata = new Dictionary<string, AreaMetadata>();

            try
            {
                string areaQuery = "SELECT a.area_code, a.area_name, a.region, p.prov_name " +
                                  "FROM areas a, provinces p " +
                                  "WHERE a.prov_code = p.prov_code";

                logger.Debug($"Loading area metadata with query: {areaQuery}");

                using (var cmd = new OleDbCommand(areaQuery, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    int mappingCount = 0;
                    while (reader.Read())
                    {
                        string areaCode = GetColumnValue(reader, "area_code");

                        if (!string.IsNullOrEmpty(areaCode) && !areaMetadata.ContainsKey(areaCode))
                        {
                            areaMetadata[areaCode] = new AreaMetadata
                            {
                                AreaName = GetColumnValue(reader, "area_name"),
                                Region = GetColumnValue(reader, "region"),
                                ProvinceName = GetColumnValue(reader, "prov_name")
                            };
                            mappingCount++;
                        }
                    }
                    logger.Debug($"Processed {mappingCount} area metadata entries");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading area metadata from database");
                throw;
            }

            return areaMetadata;
        }

        // Helper methods to safely get column values
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

        // Helper class for area metadata
        private class AreaMetadata
        {
            public string AreaName { get; set; }
            public string Region { get; set; }
            public string ProvinceName { get; set; }
        }
    }
}