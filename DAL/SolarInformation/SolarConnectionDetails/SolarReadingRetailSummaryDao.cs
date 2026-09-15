using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.SolarInformation.SolarConnectionDetails
{
    public class SolarReadingRetailSummaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        /// <summary>
        /// Gets the Solar Reading Summary Report
        /// This returns two sections: Regular tariffs and GP-3/GP-4 tariffs
        /// </summary>
        public List<SolarReadingSummaryModel> GetSolarReadingSummaryReport(RetailDetailedRequest request)
        {
            var results = new List<SolarReadingSummaryModel>();

            try
            {
                logger.Info("=== START GetSolarReadingSummaryReport ===");
                logger.Info($"Request: {request.CycleType}Cycle={request.BillCycle ?? request.CalcCycle}, NetType={request.NetType}, ReportType={request.ReportType}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Get regular tariffs (excluding 'GV1UV', 'GV1SH')
                    var regularTariffs = GetRegularTariffsSummary(conn, request);
                    logger.Info($"Retrieved {regularTariffs.Count} regular tariff records");
                    results.AddRange(regularTariffs);

                    // Get GP-3 and GP-4 tariffs
                    var gpTariffs = GetGPTariffsSummary(conn, request);
                    logger.Info($"Retrieved {gpTariffs.Count} GP tariff records");
                    results.AddRange(gpTariffs);
                }

                logger.Info($"=== END GetSolarReadingSummaryReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching solar reading summary report");
                throw;
            }
        }

        /// <summary>
        /// Gets summary for regular tariffs (excluding 'GV1UV', 'GV1SH')
        /// </summary>
        private List<SolarReadingSummaryModel> GetRegularTariffsSummary(OleDbConnection conn, RetailDetailedRequest request)
        {
            var results = new List<SolarReadingSummaryModel>();
            string sql = BuildRegularTariffQuery(request);

            logger.Debug($"Regular Tariff query SQL: {sql}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 300; // 5 minutes
                AddParameters(cmd, request);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new SolarReadingSummaryModel
                        {
                            Category = GetColumnValue(reader, "tariff_class"),
                            Tariff = GetColumnValue(reader, "tariff_code"),
                            NoOfCustomers = GetIntValue(reader, 2), // count(*)
                            ExportUnits = GetDecimalValue(reader, 3), // sum(n.units_out)
                            ImportUnits = GetDecimalValue(reader, 4), // sum(n.units_in)
                            UnitsBill = GetDecimalValue(reader, 5), // sum(n.units_bill)
                            Payments = GetDecimalValue(reader, 6), // sum(n.kwh_sales)
                            ErrorMessage = string.Empty
                        };

                        results.Add(model);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets summary for GP-3 and GP-4 tariffs
        /// </summary>
        private List<SolarReadingSummaryModel> GetGPTariffsSummary(OleDbConnection conn, RetailDetailedRequest request)
        {
            var results = new List<SolarReadingSummaryModel>();
            string sql = BuildGPTariffQuery(request);

            logger.Debug($"GP Tariff query SQL: {sql}");

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 300; // 5 minutes
                AddParameters(cmd, request);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new SolarReadingSummaryModel
                        {
                            Category = GetColumnValue(reader, "tariff_class"),
                            Tariff = GetColumnValue(reader, "tariff_code"),
                            NoOfCustomers = GetIntValue(reader, 2), // count(*)
                            ExportUnits = GetDecimalValue(reader, 3), // sum(n.units_out)
                            ImportUnits = GetDecimalValue(reader, 4), // sum(n.units_in)
                            UnitsBill = GetDecimalValue(reader, 5), // sum(n.units_bill)
                            Payments = GetDecimalValue(reader, 6), // sum(n.kwh_sales)
                            ErrorMessage = string.Empty
                        };

                        results.Add(model);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Builds query for regular tariffs (excluding 'GV1UV', 'GV1SH')
        /// </summary>
        private string BuildRegularTariffQuery(RetailDetailedRequest request)
        {
            string cycleField = request.CycleType == "A" ? "bill_cycle" : "calc_cycle";
            string netTypeCondition = BuildNetTypeCondition(request.NetType);
            string baseQuery = string.Empty;

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = $@"SELECT t.tariff_class, t.tariff_code, COUNT(*), SUM(n.units_out), 
                                  SUM(n.units_in), SUM(n.units_bill), SUM(n.kwh_sales) 
                                  FROM netmtcons n, tariff_code t 
                                  WHERE n.area_code=? 
                                  AND n.tariff_code=t.tariff_code 
                                  AND t.tariff_class NOT IN ('GV1UV','GV1SH') 
                                  AND {netTypeCondition.Replace("net_type", "n.net_type")} 
                                  AND n.{cycleField}=? 
                                  GROUP BY t.tariff_class, t.tariff_code 
                                  ORDER BY t.tariff_class, t.tariff_code";
                    break;

                case SolarReportType.Province:
                    baseQuery = $@"SELECT t.tariff_class, t.tariff_code, COUNT(*), SUM(n.units_out), 
                                  SUM(n.units_in), SUM(n.units_bill), SUM(n.kwh_sales) 
                                  FROM netmtcons n, areas a, tariff_code t 
                                  WHERE n.area_code=a.area_code 
                                  AND a.prov_code=? 
                                  AND n.tariff_code=t.tariff_code 
                                  AND t.tariff_class NOT IN ('GV1UV','GV1SH') 
                                  AND {netTypeCondition.Replace("net_type", "n.net_type")} 
                                  AND n.{cycleField}=? 
                                  GROUP BY t.tariff_class, t.tariff_code 
                                  ORDER BY t.tariff_class, t.tariff_code";
                    break;

                case SolarReportType.Region:
                    baseQuery = $@"SELECT t.tariff_class, t.tariff_code, COUNT(*), SUM(n.units_out), 
                                  SUM(n.units_in), SUM(n.units_bill), SUM(n.kwh_sales) 
                                  FROM netmtcons n, areas a, tariff_code t 
                                  WHERE n.area_code=a.area_code 
                                  AND a.region=? 
                                  AND n.tariff_code=t.tariff_code 
                                  AND t.tariff_class NOT IN ('GV1UV','GV1SH') 
                                  AND {netTypeCondition.Replace("net_type", "n.net_type")} 
                                  AND n.{cycleField}=? 
                                  GROUP BY t.tariff_class, t.tariff_code 
                                  ORDER BY t.tariff_class, t.tariff_code";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = $@"SELECT t.tariff_class, t.tariff_code, COUNT(*), SUM(n.units_out), 
                                  SUM(n.units_in), SUM(n.units_bill), SUM(n.kwh_sales) 
                                  FROM netmtcons n, tariff_code t 
                                  WHERE n.tariff_code=t.tariff_code 
                                  AND t.tariff_class NOT IN ('GV1UV','GV1SH') 
                                  AND {netTypeCondition.Replace("net_type", "n.net_type")} 
                                  AND n.{cycleField}=? 
                                  GROUP BY t.tariff_class, t.tariff_code 
                                  ORDER BY t.tariff_class, t.tariff_code";
                    break;
            }

            return baseQuery;
        }

        /// <summary>
        /// Builds query for GP-3 and GP-4 tariffs
        /// </summary>
        private string BuildGPTariffQuery(RetailDetailedRequest request)
        {
            string cycleField = request.CycleType == "A" ? "bill_cycle" : "calc_cycle";
            string netTypeCondition = BuildNetTypeCondition(request.NetType);
            string baseQuery = string.Empty;

            switch (request.ReportType)
            {
                case SolarReportType.Area:
                    baseQuery = $@"SELECT t.tariff_class, t.tariff_code, COUNT(*), SUM(n.units_out), 
                                  SUM(n.units_in), SUM(n.units_bill), SUM(n.kwh_sales) 
                                  FROM netmtcons n, tariff_code t 
                                  WHERE n.area_code=? 
                                  AND n.tariff_code=t.tariff_code 
                                  AND t.tariff_type IN ('GP-3','GP-4') 
                                  AND {netTypeCondition.Replace("net_type", "n.net_type")} 
                                  AND n.{cycleField}=? 
                                  GROUP BY t.tariff_class, t.tariff_code 
                                  ORDER BY t.tariff_class, t.tariff_code";
                    break;

                case SolarReportType.Province:
                    baseQuery = $@"SELECT t.tariff_class, t.tariff_code, COUNT(*), SUM(n.units_out), 
                                  SUM(n.units_in), SUM(n.units_bill), SUM(n.kwh_sales) 
                                  FROM netmtcons n, areas a, tariff_code t 
                                  WHERE n.area_code=a.area_code 
                                  AND a.prov_code=? 
                                  AND n.tariff_code=t.tariff_code 
                                  AND t.tariff_type IN ('GP-3','GP-4') 
                                  AND {netTypeCondition.Replace("net_type", "n.net_type")} 
                                  AND n.{cycleField}=? 
                                  GROUP BY t.tariff_class, t.tariff_code 
                                  ORDER BY t.tariff_class, t.tariff_code";
                    break;

                case SolarReportType.Region:
                    baseQuery = $@"SELECT t.tariff_class, t.tariff_code, COUNT(*), SUM(n.units_out), 
                                  SUM(n.units_in), SUM(n.units_bill), SUM(n.kwh_sales) 
                                  FROM netmtcons n, areas a, tariff_code t 
                                  WHERE n.area_code=a.area_code 
                                  AND a.region=? 
                                  AND n.tariff_code=t.tariff_code 
                                  AND t.tariff_type IN ('GP-3','GP-4') 
                                  AND {netTypeCondition.Replace("net_type", "n.net_type")} 
                                  AND n.{cycleField}=? 
                                  GROUP BY t.tariff_class, t.tariff_code 
                                  ORDER BY t.tariff_class, t.tariff_code";
                    break;

                case SolarReportType.EntireCEB:
                default:
                    baseQuery = $@"SELECT t.tariff_class, t.tariff_code, COUNT(*), SUM(n.units_out), 
                                  SUM(n.units_in), SUM(n.units_bill), SUM(n.kwh_sales) 
                                  FROM netmtcons n, tariff_code t 
                                  WHERE n.tariff_code=t.tariff_code 
                                  AND t.tariff_type IN ('GP-3','GP-4') 
                                  AND {netTypeCondition.Replace("net_type", "n.net_type")} 
                                  AND n.{cycleField}=? 
                                  GROUP BY t.tariff_class, t.tariff_code 
                                  ORDER BY t.tariff_class, t.tariff_code";
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

        private string GetColumnValue(OleDbDataReader reader, int index)
        {
            try
            {
                var value = reader[index];
                return value == DBNull.Value ? null : value.ToString()?.Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column index '{index}' not found in result set");
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

        private int GetIntValue(OleDbDataReader reader, int index)
        {
            try
            {
                var value = reader[index];
                return value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column index '{index}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid int format in column index '{index}'");
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

        private decimal GetDecimalValue(OleDbDataReader reader, int index)
        {
            try
            {
                var value = reader[index];
                return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column index '{index}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column index '{index}'");
                return 0;
            }
        }
    }
}