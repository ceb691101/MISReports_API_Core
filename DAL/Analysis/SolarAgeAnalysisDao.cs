using MISReports_Api.DBAccess;
using MISReports_Api.Models.Analysis;
using NLog;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MISReports_Api.DAL.Analysis
{
    public class SolarAgeAnalysisDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public SolarAgeAnalysisDao()
        {
            // DBConnection validates both InformixBulkConnection and InformixConnection.
            // The solar-age lookup uses the ordinary connection, which is the same pattern used elsewhere in the project.
        }

        /// <summary>
        /// Fetches the province billing data source for a given area code.
        /// Returns prov_b_svr directly as the Data Source value, which is how
        /// the original VB implementation used it (e.g. "billhsbhq@bulkinfdb1").
        /// </summary>
        private string GetProvinceDatabaseServer(string areaCode)
        {
            string sql = @"SELECT p.prov_b_svr
                             FROM prov_servers p, areas a
                            WHERE TRIM(a.area_code) = ?
                              AND p.prov_code = a.prov_code";

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", areaCode.Trim());

                        object result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value || string.IsNullOrWhiteSpace(result.ToString()))
                        {
                            throw new InvalidOperationException($"No province server found for area code '{areaCode}'.");
                        }

                        string serverName = result.ToString().Trim();
                        logger.Info($"Found province server for area '{areaCode}': '{serverName}'");
                        return serverName;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error fetching province server for area code '{areaCode}'.");
                throw;
            }
        }

        /// <summary>
        /// Builds a dynamic Informix OLE DB connection string using the InformixBillingTemplate,
        /// substituting prov_b_svr as the Data Source value directly.
        /// </summary>
        private string BuildConnectionString(string dataSource)
        {
            var templateSetting = ConfigurationManager.ConnectionStrings["InformixBillingTemplate"];
            if (templateSetting == null || string.IsNullOrWhiteSpace(templateSetting.ConnectionString))
            {
                throw new ConfigurationErrorsException("InformixBillingTemplate connection string is missing from Web.config.");
            }

            if (string.IsNullOrWhiteSpace(dataSource))
            {
                throw new ArgumentException("Data source is required.");
            }

            // Normalize dataSource: ensure it has the form "<database>@<server>".
            // If only server is provided (no @), assume the billing database name "billing".
            // If the database part looks like a POS DB (starts with "pos"), replace it with "billing".
            string ds = dataSource.Trim();
            try
            {
                if (!ds.Contains("@"))
                {
                    ds = $"billing@{ds}";
                }
                else
                {
                    var parts = ds.Split(new[] { '@' }, 2);
                    var dbPart = parts[0];
                    var srvPart = parts[1];
                    if (!string.IsNullOrWhiteSpace(dbPart) && dbPart.Trim().ToLowerInvariant().StartsWith("pos"))
                    {
                        dbPart = "billing";
                        ds = dbPart + "@" + srvPart;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Failed to normalize data source '{dataSource}'; using original value.");
                ds = dataSource.Trim();
            }

            string connStr = string.Format(templateSetting.ConnectionString, ds);
            logger.Info($"Built connection string with data source '{ds}'");
            return connStr;
        }

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        public bool TestAreaConnection(string areaCode, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                ValidateAreaCode(areaCode);

                string serverName = GetProvinceDatabaseServer(areaCode);
                string billingConnectionString = BuildConnectionString(serverName);

                using (var conn = new OleDbConnection(billingConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                logger.Error(ex, $"Error testing solar age database connection for area code {areaCode}");
                return false;
            }
        }

        public SolarAgeBillCycleResult GetBillCycles(string areaCode, int take = 20)
        {
            var result = new SolarAgeBillCycleResult
            {
                AreaCode = areaCode,
                MaxBillCycle = string.Empty,
                BillCycles = new List<SolarAgeBillCycleModel>(),
                ErrorMessage = string.Empty
            };

            try
            {
                ValidateAreaCode(areaCode);

                int cycleCount = take <= 0 ? 20 : take;

                // Both the areas lookup and yr_mnth table live on Billsmry (the main
                // Informix connection), NOT on the province billing server.
                // The province server is only needed for customer-level queries.
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string areaCycleSql = "SELECT bill_cycle FROM areas WHERE area_code = ?";
                    string areaBillCycle;

                    using (var cmd = new OleDbCommand(areaCycleSql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", areaCode.Trim());

                        object scalar = cmd.ExecuteScalar();
                        areaBillCycle = scalar == null || scalar == DBNull.Value ? string.Empty : scalar.ToString().Trim();
                    }

                    if (string.IsNullOrWhiteSpace(areaBillCycle))
                    {
                        result.ErrorMessage = $"No bill cycle found for area code {areaCode}.";
                        return result;
                    }

                    result.MaxBillCycle = areaBillCycle;

                    string billCycleSql = @"SELECT bill_cycle, bill_mnth
											FROM yr_mnth
											WHERE bill_cycle <= ?
											ORDER BY bill_cycle DESC";

                    using (var cmd = new OleDbCommand(billCycleSql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", areaBillCycle);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read() && result.BillCycles.Count < cycleCount)
                            {
                                string billCycle = GetStringValue(reader, 0);
                                string billMonth = GetStringValue(reader, 1);

                                if (string.IsNullOrWhiteSpace(billCycle))
                                {
                                    continue;
                                }

                                result.BillCycles.Add(new SolarAgeBillCycleModel
                                {
                                    BillCycle = billCycle,
                                    BillMnth = billMonth
                                });
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar age bill cycles");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public SolarAgeAnalysisResult GetAgeAnalysis(SolarAgeAnalysisRequest request)
        {
            var result = new SolarAgeAnalysisResult
            {
                AreaCode = request?.AreaCode,
                BillCycle = request?.BillCycle,
                AgeBand = request?.AgeBand,
                Records = new List<SolarAgeCustomerModel>(),
                ErrorMessage = string.Empty
            };

            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                ValidateAreaCode(request.AreaCode);

                if (string.IsNullOrWhiteSpace(request.BillCycle) || !IsDigitsOnly(request.BillCycle))
                {
                    throw new ArgumentException("Bill cycle is required and must be numeric.");
                }

                // Dynamically fetch the province server for this area code
                string serverName = GetProvinceDatabaseServer(request.AreaCode);
                string billingConnectionString = BuildConnectionString(serverName);

                string ageFilter = BuildAgeFilterClause(request.AgeBand);
                string tableName = $"electric_{request.AreaCode.Trim()}";
                string sql = $@"SELECT n.acct_number,
									   n.net_type,
									   c.cust_fname,
									   c.cust_lname,
									   c.address_1,
									   c.address_2,
									   c.address_3,
									   m.agrmnt_date
								FROM netmtcons n,
									 {tableName} e,
									 netmeter m,
									 customers c
								WHERE n.bill_cycle = ?
								  AND n.area_code = ?
								  AND e.cust_status <> '9'
								  AND n.acct_number = e.acct_number
								  AND m.acct_number = e.acct_number
								  AND c.acct_number = e.acct_number
								  AND {ageFilter}
								ORDER BY m.agrmnt_date";

                using (var conn = new OleDbConnection(billingConnectionString))
                {
                    conn.Open();

                    // -------------------------------------------------------
                    // 1. Summary query: fetch agrmnt_date for ALL records
                    //    (no age-band filter) so AgeBandCounts always reflects
                    //    the full picture, matching the original VB summary report.
                    // -------------------------------------------------------
                    var counts = new Dictionary<string, int>
                    {
                        { "<=1", 0 },
                        { "1-2", 0 },
                        { "2-3", 0 },
                        { "3-4", 0 },
                        { "4-5", 0 },
                        { "5-6", 0 },
                        { "6-7", 0 },
                        { "7-8", 0 },
                        { ">8", 0 },
                        { "null", 0 }
                    };

                    string summarySql = $@"SELECT m.agrmnt_date
                                            FROM netmtcons n,
                                                 {tableName} e,
                                                 netmeter m,
                                                 customers c
                                           WHERE n.bill_cycle = ?
                                             AND n.area_code = ?
                                             AND e.cust_status <> '9'
                                             AND n.acct_number = e.acct_number
                                             AND m.acct_number = e.acct_number
                                             AND c.acct_number = e.acct_number";

                    using (var summaryCmd = new OleDbCommand(summarySql, conn))
                    {
                        summaryCmd.CommandTimeout = 300;
                        summaryCmd.Parameters.AddWithValue("?", request.BillCycle.Trim());
                        summaryCmd.Parameters.AddWithValue("?", request.AreaCode.Trim());

                        using (var summaryReader = summaryCmd.ExecuteReader())
                        {
                            while (summaryReader.Read())
                            {
                                string dateText = GetStringValue(summaryReader, 0);
                                DateTime? agreementDate = ParseDate(dateText);

                                if (!agreementDate.HasValue)
                                {
                                    counts["null"] += 1;
                                }
                                else
                                {
                                    long days = (long)Math.Max(0, (DateTime.Today.Date - agreementDate.Value.Date).TotalDays);

                                    if (days <= 365) counts["<=1"] += 1;
                                    else if (days <= 730) counts["1-2"] += 1;
                                    else if (days <= 1095) counts["2-3"] += 1;
                                    else if (days <= 1460) counts["3-4"] += 1;
                                    else if (days <= 1825) counts["4-5"] += 1;
                                    else if (days <= 2190) counts["5-6"] += 1;
                                    else if (days <= 2555) counts["6-7"] += 1;
                                    else if (days <= 2920) counts["7-8"] += 1;
                                    else counts[">8"] += 1;
                                }
                            }
                        }
                    }

                    result.AgeBandCounts = counts;

                    // -------------------------------------------------------
                    // 2. Detail query: fetch full customer records filtered
                    //    by the requested age band.
                    // -------------------------------------------------------
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300;
                        cmd.Parameters.AddWithValue("?", request.BillCycle.Trim());
                        cmd.Parameters.AddWithValue("?", request.AreaCode.Trim());

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string agreementDateText = GetStringValue(reader, 7);
                                DateTime? agreementDate = ParseDate(agreementDateText);

                                long ageDays = agreementDate.HasValue
                                    ? (long)Math.Max(0, (DateTime.Today.Date - agreementDate.Value.Date).TotalDays)
                                    : 0;

                                result.Records.Add(new SolarAgeCustomerModel
                                {
                                    AccountNumber = GetStringValue(reader, 0),
                                    NetTypeCode = GetStringValue(reader, 1),
                                    NetType = GetNetTypeDisplayName(GetStringValue(reader, 1)),
                                    PanelCapacity = GetStringValue(reader, 8),
                                    CustomerFirstName = GetStringValue(reader, 2),
                                    CustomerLastName = GetStringValue(reader, 3),
                                    Address1 = GetStringValue(reader, 4),
                                    Address2 = GetStringValue(reader, 5),
                                    Address3 = GetStringValue(reader, 6),
                                    AgreementDate = agreementDate.HasValue
                                        ? agreementDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                                        : agreementDateText,
                                    AgeDays = ageDays
                                });
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar age analysis data");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public SolarAgeAnalysisResult GetFullReport(SolarAgeAnalysisRequest request)
        {
            var result = new SolarAgeAnalysisResult
            {
                AreaCode = request?.AreaCode,
                BillCycle = request?.BillCycle,
                AgeBand = "all",
                Records = new List<SolarAgeCustomerModel>(),
                ErrorMessage = string.Empty
            };

            try
            {
                if (request == null)
                {
                    throw new ArgumentNullException(nameof(request));
                }

                ValidateAreaCode(request.AreaCode);

                if (string.IsNullOrWhiteSpace(request.BillCycle) || !IsDigitsOnly(request.BillCycle))
                {
                    throw new ArgumentException("Bill cycle is required and must be numeric.");
                }

                string serverName = GetProvinceDatabaseServer(request.AreaCode);
                string billingConnectionString = BuildConnectionString(serverName);

                string tableName = $"electric_{request.AreaCode.Trim()}";
                string sql = $@"SELECT n.acct_number,
                                       n.net_type,
                                       c.cust_fname,
                                       c.cust_lname,
                                       c.address_1,
                                       c.address_2,
                                       c.address_3,
                                       m.agrmnt_date,
                                       m.gen_cap
                                FROM netmtcons n,
                                     {tableName} e,
                                     netmeter m,
                                     customers c
                                WHERE n.bill_cycle = ?
                                  AND n.area_code = ?
                                  AND e.cust_status <> '9'
                                  AND n.acct_number = e.acct_number
                                  AND m.acct_number = e.acct_number
                                  AND c.acct_number = e.acct_number
                                ORDER BY n.net_type, m.agrmnt_date";

                using (var conn = new OleDbConnection(billingConnectionString))
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300;
                        cmd.Parameters.AddWithValue("?", request.BillCycle.Trim());
                        cmd.Parameters.AddWithValue("?", request.AreaCode.Trim());

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string agreementDateText = GetStringValue(reader, 7);
                                DateTime? agreementDate = ParseDate(agreementDateText);

                                result.Records.Add(new SolarAgeCustomerModel
                                {
                                    AccountNumber = GetStringValue(reader, 0),
                                    NetTypeCode = GetStringValue(reader, 1),
                                    NetType = GetNetTypeDisplayName(GetStringValue(reader, 1)),
                                    PanelCapacity = GetStringValue(reader, 8),
                                    CustomerFirstName = GetStringValue(reader, 2),
                                    CustomerLastName = GetStringValue(reader, 3),
                                    Address1 = GetStringValue(reader, 4),
                                    Address2 = GetStringValue(reader, 5),
                                    Address3 = GetStringValue(reader, 6),
                                    AgreementDate = agreementDate.HasValue
                                        ? agreementDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                                        : agreementDateText,
                                    AgeDays = agreementDate.HasValue
                                        ? (long)Math.Max(0, (DateTime.Today.Date - agreementDate.Value.Date).TotalDays)
                                        : 0
                                });
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching solar age full report data");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private static void ValidateAreaCode(string areaCode)
        {
            if (string.IsNullOrWhiteSpace(areaCode))
            {
                throw new ArgumentException("Area code is required.");
            }

            if (!IsDigitsOnly(areaCode))
            {
                throw new ArgumentException("Area code must contain digits only.");
            }
        }

        private static bool IsDigitsOnly(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value.Trim(), "^[0-9]+$");
        }

        private static string BuildAgeFilterClause(string ageBand)
        {
            string normalized = (ageBand ?? string.Empty).Trim().ToLowerInvariant();

            switch (normalized)
            {
                case "all":
                case "":
                    return "1 = 1";
                case "null":
                case "agreement null":
                case "null agreement":
                    return "m.agrmnt_date IS NULL";
                case "<=1":
                case "1":
                case "1y":
                case "0-1":
                case "le1":
                    return "(TODAY - m.agrmnt_date) <= 365";
                case "1-2":
                case ">1<=2":
                case "gt1le2":
                    return "(TODAY - m.agrmnt_date) > 365 AND (TODAY - m.agrmnt_date) <= 730";
                case "2-3":
                case ">2<=3":
                case "gt2le3":
                    return "(TODAY - m.agrmnt_date) > 730 AND (TODAY - m.agrmnt_date) <= 1095";
                case "3-4":
                case ">3<=4":
                case "gt3le4":
                    return "(TODAY - m.agrmnt_date) > 1095 AND (TODAY - m.agrmnt_date) <= 1460";
                case "4-5":
                case ">4<=5":
                case "gt4le5":
                    return "(TODAY - m.agrmnt_date) > 1460 AND (TODAY - m.agrmnt_date) <= 1825";
                case "5-6":
                case ">5<=6":
                case "gt5le6":
                    return "(TODAY - m.agrmnt_date) > 1825 AND (TODAY - m.agrmnt_date) <= 2190";
                case "6-7":
                case ">6<=7":
                case "gt6le7":
                    return "(TODAY - m.agrmnt_date) > 2190 AND (TODAY - m.agrmnt_date) <= 2555";
                case "7-8":
                case ">7<=8":
                case "gt7le8":
                    return "(TODAY - m.agrmnt_date) > 2555 AND (TODAY - m.agrmnt_date) <= 2920";
                case ">8":
                case "8+":
                case "gt8":
                    return "(TODAY - m.agrmnt_date) > 2920";
                default:
                    throw new ArgumentException("Invalid age band. Supported values are: All, Null, <=1, 1-2, 2-3, 3-4, 4-5, 5-6, 6-7, 7-8, >8.");
            }
        }

        private static string GetNetTypeDisplayName(string netType)
        {
            switch ((netType ?? string.Empty).Trim())
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
                    return string.Empty;
            }
        }

        private static string GetStringValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                return reader.IsDBNull(ordinal) ? string.Empty : reader.GetValue(ordinal)?.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTime.TryParse(value, out var parsedDate))
            {
                return parsedDate;
            }

            return null;
        }
    }
}