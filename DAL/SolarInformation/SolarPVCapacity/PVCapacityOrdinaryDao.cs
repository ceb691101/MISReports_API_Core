using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using NLog;

namespace MISReports_Api.DAL.SolarInformation.SolarPVCapacity
{
    public class PVCapacityOrdinaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                logger.Debug("=== Testing Connection ===");
                logger.Debug($"Connection String: {_dbConnection.OrdinaryConnectionString}");

                var builder = new OleDbConnectionStringBuilder(_dbConnection.OrdinaryConnectionString);
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

                using (var conn = _dbConnection.GetConnection(false))
                {
                    logger.Debug("Attempting to open connection...");
                    conn.Open();
                    logger.Info("Database connection opened successfully.");
                    return true;
                }
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "OLE DB Error occurred during connection test");

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
                logger.Error(ex, "General error occurred during connection test");
                errorMessage = $"Connection test failed: {ex.Message}";
                return false;
            }
        }

        public List<PVCapacityModel> GetPVCapacityReport(SolarProgressRequest request)
        {
            var results = new List<PVCapacityModel>();

            try
            {
                logger.Info("=== START GetPVCapacityReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, ReportType={request.ReportType}, AreaCode={request.AreaCode}, ProvCode={request.ProvCode}, Region={request.Region}");

                string sql = BuildSqlQuery(request);
                logger.Debug($"Generated SQL: {sql}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    logger.Debug("Opening database connection...");
                    conn.Open();
                    logger.Debug("Database connection opened successfully.");

                    logger.Debug("Loading area metadata mapping...");
                    var areaMetadata = GetAreaMetadata(conn);
                    logger.Info($"Loaded {areaMetadata.Count} area metadata entries");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        logger.Debug("Adding parameter: BillCycle = {BillCycle}", request.BillCycle);
                        cmd.Parameters.AddWithValue("", request.BillCycle);

                        if (request.ReportType == SolarReportType.Area && !string.IsNullOrEmpty(request.AreaCode))
                        {
                            logger.Debug("Adding parameter: AreaCode = {AreaCode}", request.AreaCode);
                            cmd.Parameters.AddWithValue("", request.AreaCode);
                        }
                        else if (request.ReportType == SolarReportType.Province && !string.IsNullOrEmpty(request.ProvCode))
                        {
                            logger.Debug("Adding parameter: ProvCode = {ProvCode}", request.ProvCode);
                            cmd.Parameters.AddWithValue("", request.ProvCode);
                        }
                        else if (request.ReportType == SolarReportType.Region && !string.IsNullOrEmpty(request.Region))
                        {
                            logger.Debug("Adding parameter: Region = {Region}", request.Region);
                            cmd.Parameters.AddWithValue("", request.Region);
                        }

                        logger.Debug("Executing query...");
                        using (var reader = cmd.ExecuteReader())
                        {
                            logger.Debug("Reading results...");
                            int recordCount = 0;

                            while (reader.Read())
                            {
                                recordCount++;
                                var pvCapacity = MapPVCapacityFromReader(reader, areaMetadata, request.ReportType);
                                results.Add(pvCapacity);
                            }

                            logger.Info($"Successfully read {recordCount} records from database");
                        }
                    }
                }

                logger.Info("=== END GetPVCapacityReport (Success) ===");
                logger.Info($"Returning {results.Count} results");
                return results;
            }
            catch (OleDbException ex)
            {
                logger.Error(ex, "Database error occurred while fetching PV capacity report");
                logger.Error($"SQL: {BuildSqlQuery(request)}");
                throw new ApplicationException("Database error occurred while fetching report", ex);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error occurred while fetching PV capacity report");
                throw;
            }
        }

        private string BuildSqlQuery(SolarProgressRequest request)
        {
            string baseQuery;

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = "SELECT area_code, bill_cycle, net_type, cnt, tot_gen_cap " +
                               "FROM netprogrs " +
                               "WHERE bill_cycle = ? AND area_code = ? " +
                               "ORDER BY net_type";
                    break;

                case SolarReportType.Province:
                    baseQuery = "SELECT n.area_code, n.bill_cycle, n.net_type, SUM(n.cnt) AS cnt, SUM(n.tot_gen_cap) AS tot_gen_cap " +
                               "FROM netprogrs n, areas a " +
                               "WHERE n.bill_cycle = ? AND a.area_code = n.area_code AND a.prov_code = ? " +
                               "GROUP BY n.area_code, n.bill_cycle, n.net_type " +
                               "ORDER BY n.bill_cycle, n.area_code, n.net_type";
                    break;

                case SolarReportType.Region:
                    baseQuery = "SELECT n.area_code, n.bill_cycle, n.net_type, SUM(n.cnt) AS cnt, SUM(n.tot_gen_cap) AS tot_gen_cap " +
                               "FROM netprogrs n, areas a " +
                               "WHERE n.bill_cycle = ? AND a.area_code = n.area_code AND a.region = ? " +
                               "GROUP BY n.area_code, n.bill_cycle, n.net_type " +
                               "ORDER BY n.bill_cycle, n.area_code, n.net_type";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = "SELECT area_code, bill_cycle, net_type, SUM(cnt) AS cnt, SUM(tot_gen_cap) AS tot_gen_cap " +
                               "FROM netprogrs " +
                               "WHERE bill_cycle = ? " +
                               "GROUP BY area_code, bill_cycle, net_type " +
                               "ORDER BY bill_cycle, area_code, net_type";
                    break;
            }

            return baseQuery;
        }

        private PVCapacityModel MapPVCapacityFromReader(OleDbDataReader reader, Dictionary<string, AreaMetadata> areaMetadata, SolarReportType reportType)
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

                string areaCode = GetColumnValue(reader, "area_code");
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