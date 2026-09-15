using MISReports_Api.DBAccess;
using MISReports_Api.Models.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.Collection
{
    public class CustomersHighestOutstandingDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private class AreaInfo
        {
            public string AreaCode { get; set; }
            public string AreaName { get; set; }
            public string ProvCode { get; set; }
            public string Province { get; set; }
        }

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        public List<CustomersHighestOutstandingModel> GetReportData(CustomersHighestOutstandingRequest request)
        {
            logger.Info("=== START CustomersHighestOutstanding GetReportData ===");
            var finalResults = new List<CustomersHighestOutstandingModel>();

            try
            {
                // 1. Fetch areas and their associated province codes based on the requested scope
                List<AreaInfo> selectedAreas = FetchSelectedAreas(request);
                if (selectedAreas.Count == 0)
                {
                    logger.Warn($"No areas found for scope: {request.Scope}, ProvinceCode: {request.ProvinceCode}, RegionCode: {request.RegionCode}");
                    return finalResults;
                }

                logger.Info($"Found {selectedAreas.Count} areas to query.");

                // 2. Group areas by province code so we can pool database connections per province server
                var areasByProvince = selectedAreas.GroupBy(a => a.ProvCode);

                foreach (var provinceGroup in areasByProvince)
                {
                    string provCode = provinceGroup.Key;
                    var areasInProvince = provinceGroup.ToList();

                    logger.Info($"Processing province code: {provCode} containing {areasInProvince.Count} areas.");

                    string serverName = GetProvinceDatabaseServer(provCode);
                    if (string.IsNullOrEmpty(serverName))
                    {
                        logger.Warn($"Could not find province database server for province code: {provCode}. Skipping.");
                        continue;
                    }

                    string billingConnectionString = BuildConnectionString(serverName);
                    if (string.IsNullOrEmpty(billingConnectionString))
                    {
                        logger.Warn($"Could not construct connection string for server: {serverName}. Skipping.");
                        continue;
                    }

                    // 3. Query all area electric tables in this province database
                    using (var conn = new OleDbConnection(billingConnectionString))
                    {
                        try
                        {
                            conn.Open();

                            foreach (var area in areasInProvince)
                            {
                                string tableName = $"electric_{area.AreaCode.Trim()}";
                                logger.Info($"Querying table {tableName} in {serverName} for area {area.AreaName}");

                                string sql = $@"
                                    SELECT a.acct_number, 
                                           b.cust_fname, 
                                           b.cust_lname, 
                                           b.address_1, 
                                           b.address_2, 
                                           b.tel_no, 
                                           a.lst_csh_dt, 
                                           a.crnt_rd_dt, 
                                           a.crnt_balance, 
                                           a.kwh_charge, 
                                           a.tariff_code, 
                                           ROUND((a.crnt_balance / (a.kwh_charge + 0.0001)), 1) as arrears_months,
                                           a.crnt_cons as units
                                    FROM {tableName} a, customers b
                                    WHERE a.acct_number = b.acct_number
                                      AND (a.crnt_balance / (a.kwh_charge + 0.0001)) > ?
                                      AND a.cust_status = '1'
                                      AND a.crnt_balance > ?
                                    ORDER BY a.crnt_balance DESC";

                                using (var cmd = new OleDbCommand(sql, conn))
                                {
                                    cmd.CommandTimeout = 180; // Allow sufficient time for large queries
                                    cmd.Parameters.AddWithValue("?", (double)request.MonthsInArrears);
                                    cmd.Parameters.AddWithValue("?", (double)request.OutstandingBalance);

                                    try
                                    {
                                        using (var reader = cmd.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                string acctNumber = reader[0] != DBNull.Value ? reader[0].ToString().Trim() : "";
                                                string firstName = reader[1] != DBNull.Value ? reader[1].ToString().Trim() : "";
                                                string lastName = reader[2] != DBNull.Value ? reader[2].ToString().Trim() : "";
                                                string customerName = (firstName + " " + lastName).Trim();

                                                string address1 = reader[3] != DBNull.Value ? reader[3].ToString().Trim() : "";
                                                string address2 = reader[4] != DBNull.Value ? reader[4].ToString().Trim() : "";
                                                string address = (address1 + " " + address2).Trim();

                                                string tel = reader[5] != DBNull.Value ? reader[5].ToString().Trim() : "";

                                                string lstCashDate = "";
                                                if (reader[6] != DBNull.Value)
                                                {
                                                    if (DateTime.TryParse(reader[6].ToString(), out DateTime cshDate))
                                                        lstCashDate = cshDate.ToString("dd-MM-yyyy");
                                                }

                                                string crntRdDate = "";
                                                if (reader[7] != DBNull.Value)
                                                {
                                                    if (DateTime.TryParse(reader[7].ToString(), out DateTime crntDate))
                                                        crntRdDate = crntDate.ToString("dd-MM-yyyy");
                                                }

                                                decimal crntBal = reader[8] != DBNull.Value ? Convert.ToDecimal(reader[8]) : 0;
                                                decimal kwhChg = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                                                string tariff = reader[10] != DBNull.Value ? reader[10].ToString().Trim() : "";
                                                decimal arrears = reader[11] != DBNull.Value ? Convert.ToDecimal(reader[11]) : 0;
                                                decimal units = reader[12] != DBNull.Value ? Convert.ToDecimal(reader[12]) : 0;

                                                finalResults.Add(new CustomersHighestOutstandingModel
                                                {
                                                    Province = !string.IsNullOrEmpty(area.Province) ? area.Province : area.ProvCode,
                                                    AreaName = area.AreaName,
                                                    AccountNumber = acctNumber,
                                                    CustomerName = customerName,
                                                    Address = address,
                                                    Telephone = tel,
                                                    LastCashDate = lstCashDate,
                                                    CurrentReadingDate = crntRdDate,
                                                    CurrentBalance = crntBal,
                                                    KwhCharge = kwhChg,
                                                    ArrearsBalance = crntBal - kwhChg,
                                                    TariffCode = tariff,
                                                    ArrearsMonths = arrears,
                                                    Units = units
                                                });
                                            }
                                        }
                                    }
                                    catch (OleDbException oleEx)
                                    {
                                        logger.Warn(oleEx, $"Table {tableName} query failed or table is missing. Skipping this area.");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, $"Failed to query billing data for province database server {serverName}. skipping entire province group.");
                        }
                    }
                }

                // 4. Order final results globally by Province ASC, AreaName ASC, then by ArrearsBalance DESC
                finalResults = finalResults.OrderBy(r => r.Province).ThenBy(r => r.AreaName).ThenByDescending(r => r.ArrearsBalance).ToList();
                logger.Info($"=== END CustomersHighestOutstanding GetReportData (Success) - {finalResults.Count} records ===");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred in CustomersHighestOutstandingDao");
                throw;
            }

            return finalResults;
        }

        private List<AreaInfo> FetchSelectedAreas(CustomersHighestOutstandingRequest request)
        {
            var areas = new List<AreaInfo>();
            string sql;

            using (var conn = _dbConnection.GetConnection(false))
            {
                conn.Open();

                if (request.Scope.Equals("Province", StringComparison.OrdinalIgnoreCase))
                {
                    sql = @"SELECT a.area_code, a.area_name, a.prov_code, p.prov_name 
                            FROM areas a 
                            LEFT JOIN prov_servers p ON TRIM(a.prov_code) = TRIM(p.prov_code) 
                            WHERE a.prov_code = ? 
                            ORDER BY a.area_name";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", request.ProvinceCode.Trim());
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                areas.Add(new AreaInfo
                                {
                                    AreaCode = reader[0]?.ToString().Trim(),
                                    AreaName = reader[1]?.ToString().Trim(),
                                    ProvCode = reader[2]?.ToString().Trim(),
                                    Province = reader[3]?.ToString().Trim()
                                });
                            }
                        }
                    }
                }
                else
                {
                    sql = @"SELECT a.area_code, a.area_name, a.prov_code, p.prov_name 
                            FROM areas a 
                            LEFT JOIN prov_servers p ON TRIM(a.prov_code) = TRIM(p.prov_code) 
                            WHERE a.region = ? 
                            ORDER BY a.area_name";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", request.RegionCode.Trim());
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                areas.Add(new AreaInfo
                                {
                                    AreaCode = reader[0]?.ToString().Trim(),
                                    AreaName = reader[1]?.ToString().Trim(),
                                    ProvCode = reader[2]?.ToString().Trim(),
                                    Province = reader[3]?.ToString().Trim()
                                });
                            }
                        }
                    }
                }
            }

            return areas;
        }

        private string GetProvinceDatabaseServer(string provCode)
        {
            string sql = "SELECT prov_b_svr FROM prov_servers WHERE prov_code = ?";

            using (var conn = _dbConnection.GetConnection(false))
            {
                conn.Open();
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", provCode.Trim());
                    object result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                    {
                        return null;
                    }
                    return result.ToString().Trim();
                }
            }
        }

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

            return string.Format(templateSetting.ConnectionString, ds);
        }
    }
}
