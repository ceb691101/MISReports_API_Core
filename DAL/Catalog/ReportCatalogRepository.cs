using MISReports_Api.Models.Catalog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.Catalog
{
    public class CategoryNameCount
    {
        public string CatName { get; set; }
        public int Count { get; set; }
    }

    public class ReportCatalogRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private static string SafeString(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;
            return value.ToString().Trim();
        }

        private static bool IsExcludedCategory(string catCode, string catName)
        {
            string code = (catCode ?? "").Trim().ToLowerInvariant();
            string name = (catName ?? "").Trim().ToLowerInvariant();

            if (code == "dashboard" || name == "dashboard" || name == "dashboard" || name == "main dashboard" ||
                code == "all reports" || name == "all reports" || code == "all" || name == "all")
            {
                return true;
            }

            return false;
        }

        public async Task<ReportCatalogResponseModel> GetAllCatalogReportsAsync(string epfNo, string roleId)
        {
            var response = new ReportCatalogResponseModel();
            var userRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(roleId))
            {
                userRoles.Add(roleId.Trim());
            }

            try
            {
                using (var conn = new OracleConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    // Step 1: If epfNo is provided, resolve user's role IDs from REP_ROLE_NEW
                    if (!string.IsNullOrWhiteSpace(epfNo))
                    {
                        const string roleSql = @"
                            SELECT DISTINCT UPPER(TRIM(r.roleid)) AS ROLEID
                            FROM REP_ROLE_NEW r
                            WHERE UPPER(TRIM(r.epf_no)) = UPPER(TRIM(:epfNo))";

                        using (var cmdRole = new OracleCommand(roleSql, conn))
                        {
                            cmdRole.BindByName = true;
                            cmdRole.Parameters.Add("epfNo", OracleDbType.Varchar2).Value = epfNo.Trim();

                            using (var readerRole = await cmdRole.ExecuteReaderAsync())
                            {
                                while (await readerRole.ReadAsync())
                                {
                                    var rId = SafeString(readerRole["ROLEID"]);
                                    if (!string.IsNullOrWhiteSpace(rId))
                                    {
                                        userRoles.Add(rId);
                                    }
                                }
                            }
                        }
                    }

                    // Step 2: Fetch assigned reports for user roles from REP_ROLES_REP_NEW
                    var accessibleReportKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (userRoles.Count > 0)
                    {
                        var roleList = userRoles.ToList();
                        var bindNames = new List<string>();
                        for (int i = 0; i < roleList.Count; i++)
                        {
                            bindNames.Add(":r" + i);
                        }

                        string accessSql = string.Format(@"
                            SELECT DISTINCT TRIM(catcode) AS CATCODE, TRIM(repid) AS REPID, TRIM(repid_no) AS REPID_NO
                            FROM REP_ROLES_REP_NEW
                            WHERE UPPER(TRIM(roleid)) IN ({0})", string.Join(",", bindNames.ToArray()));

                        using (var cmdAccess = new OracleCommand(accessSql, conn))
                        {
                            cmdAccess.BindByName = true;
                            for (int i = 0; i < roleList.Count; i++)
                            {
                                cmdAccess.Parameters.Add("r" + i, OracleDbType.Varchar2).Value = roleList[i];
                            }

                            using (var readerAccess = await cmdAccess.ExecuteReaderAsync())
                            {
                                while (await readerAccess.ReadAsync())
                                {
                                    var cat = SafeString(readerAccess["CATCODE"]);
                                    var rep = SafeString(readerAccess["REPID"]);
                                    var repNo = SafeString(readerAccess["REPID_NO"]);

                                    if (!string.IsNullOrEmpty(cat) && !string.IsNullOrEmpty(rep))
                                    {
                                        accessibleReportKeys.Add(string.Format("{0}::{1}", cat, rep));
                                    }
                                    if (!string.IsNullOrEmpty(repNo))
                                    {
                                        accessibleReportKeys.Add(repNo);
                                    }
                                }
                            }
                        }
                    }

                    // Step 2.5: Fetch parameter definitions dictionary from rep_report_params_new
                    var paramDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    try
                    {
                        const string paramSql = @"
                            SELECT UPPER(TRIM(paraname)) AS PARANAME, TRIM(description) AS DESCRIPTION
                            FROM rep_report_params_new";

                        using (var cmdParam = new OracleCommand(paramSql, conn))
                        {
                            using (var readerParam = await cmdParam.ExecuteReaderAsync())
                            {
                                while (await readerParam.ReadAsync())
                                {
                                    var pName = SafeString(readerParam["PARANAME"]);
                                    var pDesc = SafeString(readerParam["DESCRIPTION"]);
                                    if (!string.IsNullOrWhiteSpace(pName))
                                    {
                                        paramDictionary[pName] = string.IsNullOrWhiteSpace(pDesc) ? pName : pDesc;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exParam)
                    {
                        Debug.WriteLine("Warning: Failed to fetch param descriptions: " + exParam.Message);
                    }

                    // Step 3: Fetch all reports from REP_REPORTS_NEW joined with REP_CATS_NEW
                    const string sql = @"
                        SELECT 
                            TRIM(r.repid_no) AS REPID_NO,
                            TRIM(r.repid) AS REPID,
                            TRIM(r.repname) AS REPNAME,
                            TRIM(r.catcode) AS CATCODE,
                            TRIM(c.catname) AS CATNAME,
                            r.favorite AS FAVORITE,
                            r.active AS ACTIVE,
                            r.paramlist AS PARAMLIST,
                            r.description AS DESCRIPTION,
                            TRIM(r.path) AS PATH
                        FROM REP_REPORTS_NEW r
                        LEFT JOIN REP_CATS_NEW c ON TRIM(r.catcode) = TRIM(c.catcode)
                        WHERE r.active = 1 OR r.active IS NULL
                        ORDER BY c.catname, NVL(r.repname, r.repid)";

                    var categoryCounts = new Dictionary<string, CategoryNameCount>(StringComparer.OrdinalIgnoreCase);

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var repIdNo = SafeString(reader["REPID_NO"]) ?? "";
                                var repId = SafeString(reader["REPID"]) ?? "";
                                var repName = SafeString(reader["REPNAME"]);
                                if (string.IsNullOrWhiteSpace(repName))
                                {
                                    repName = repId;
                                }

                                var catCode = SafeString(reader["CATCODE"]) ?? "UNCATEGORIZED";
                                var catName = SafeString(reader["CATNAME"]);
                                if (string.IsNullOrWhiteSpace(catName))
                                {
                                    catName = catCode;
                                }

                                // Exclude Dashboard and All Reports navigation categories
                                if (IsExcludedCategory(catCode, catName))
                                {
                                    continue;
                                }

                                var description = SafeString(reader["DESCRIPTION"]) ?? "";
                                var paramList = SafeString(reader["PARAMLIST"]) ?? "";
                                var samplePath = SafeString(reader["PATH"]) ?? "";
                                int favorite = (reader["FAVORITE"] != DBNull.Value && reader["FAVORITE"] != null) ? Convert.ToInt32(reader["FAVORITE"]) : 0;
                                int active = (reader["ACTIVE"] != DBNull.Value && reader["ACTIVE"] != null) ? Convert.ToInt32(reader["ACTIVE"]) : 1;

                                bool hasAccess = accessibleReportKeys.Contains(string.Format("{0}::{1}", catCode, repId)) ||
                                                 accessibleReportKeys.Contains(repIdNo);

                                var paramDescriptions = ParseActiveParameterDescriptions(paramList, paramDictionary);

                                var item = new ReportCatalogItemModel
                                {
                                    RepIdNo = repIdNo,
                                    RepId = repId,
                                    ReportName = repName,
                                    CatCode = catCode,
                                    CategoryName = catName,
                                    Description = description,
                                    ParamList = paramList,
                                    ParameterDescriptions = paramDescriptions,
                                    Path = samplePath,
                                    Favorite = favorite,
                                    Active = active,
                                    HasAccess = hasAccess
                                };

                                response.Reports.Add(item);

                                if (!categoryCounts.ContainsKey(catCode))
                                {
                                    categoryCounts[catCode] = new CategoryNameCount { CatName = catName, Count = 0 };
                                }
                                categoryCounts[catCode].Count++;
                            }
                        }
                    }

                    // Step 4: Populate categories summary
                    foreach (var kvp in categoryCounts)
                    {
                        response.Categories.Add(new ReportCategorySummaryModel
                        {
                            CatCode = kvp.Key,
                            CategoryName = kvp.Value.CatName,
                            TotalReports = kvp.Value.Count
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in GetAllCatalogReportsAsync: " + ex.Message);
                throw;
            }

            return response;
        }

        private static List<string> ParseActiveParameterDescriptions(string paramList, Dictionary<string, string> paramDictionary)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(paramList))
            {
                return results;
            }

            string raw = paramList.Trim();
            if (raw.StartsWith("<"))
            {
                raw = System.Text.RegularExpressions.Regex.Replace(raw, @"^<[^>]*>&?", "");
            }

            var tokens = raw.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var parts = token.Split('=');
                if (parts.Length == 2)
                {
                    string pName = parts[0].Trim();
                    string val = parts[1].Trim().ToLowerInvariant();

                    if (val == "1" || val == "true" || val == "yes")
                    {
                        if (!string.IsNullOrEmpty(pName))
                        {
                            if (paramDictionary != null && paramDictionary.TryGetValue(pName, out var desc) && !string.IsNullOrWhiteSpace(desc))
                            {
                                results.Add(desc);
                            }
                            else
                            {
                                results.Add(pName);
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}
