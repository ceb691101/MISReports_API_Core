using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Admin.Report_Parameters;

namespace MISReports_Api.DAL.Admin.Report_Parameters
{
    public class RepParaRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ReportParameterListItemModel> GetAllReportParams()
        {
            const string sql = @"
SELECT paraid,
       paraname,
       description,
       CASE
           WHEN populated = 1 THEN 'Yes'
           ELSE 'No'
       END AS populated
FROM rep_report_params_new
ORDER BY paraname";

            var results = new List<ReportParameterListItemModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var populatedText = reader["POPULATED"]?.ToString()?.Trim();

                        results.Add(new ReportParameterListItemModel
                        {
                            ParaId = reader["PARAID"]?.ToString()?.Trim(),
                            ParaName = reader["PARANAME"]?.ToString()?.Trim(),
                            Description = reader["DESCRIPTION"]?.ToString()?.Trim(),
                            Populated = string.Equals(populatedText, "Yes", StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
            }

            return results;
        }

        public SaveReportParamsResultModel SaveReportParams(string paraId, string paraDesc)
        {
            const string selectSql = @"
SELECT paraname
FROM rep_report_params_new
WHERE UPPER(TRIM(paraname)) = :parm_ParaID";

            const string nextParaIdSql = @"
SELECT NVL(MAX(paraid), 0) + 1
FROM rep_report_params_new";

            const string insertSql = @"
INSERT INTO rep_report_params_new (paraid, paraname, description, populated)
VALUES (:parm_NextParaID, :parm_ParaID, :parm_ParaDesc, 0)";

            const string updateSql = @"
UPDATE rep_report_params_new
SET description = :parm_ParaDesc
WHERE UPPER(TRIM(paraname)) = :parm_ParaID";

            var normalizedParaId = paraId?.Trim()?.ToUpperInvariant();
            var normalizedParaDesc = paraDesc?.Trim();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        bool found;
                        using (var selectCmd = new OracleCommand(selectSql, conn))
                        {
                            selectCmd.BindByName = true;
                            selectCmd.Transaction = transaction;
                            selectCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                            var existing = selectCmd.ExecuteScalar();
                            found = existing != null && existing != DBNull.Value;
                        }

                        if (!found)
                        {
                            int nextParaId;
                            using (var nextParaIdCmd = new OracleCommand(nextParaIdSql, conn))
                            {
                                nextParaIdCmd.BindByName = true;
                                nextParaIdCmd.Transaction = transaction;
                                var nextValue = nextParaIdCmd.ExecuteScalar();
                                nextParaId = nextValue == null || nextValue == DBNull.Value
                                    ? 1
                                    : Convert.ToInt32(nextValue);
                            }

                            using (var insertCmd = new OracleCommand(insertSql, conn))
                            {
                                insertCmd.BindByName = true;
                                insertCmd.Transaction = transaction;
                                insertCmd.Parameters.Add("parm_NextParaID", OracleDbType.Int32).Value = nextParaId;
                                insertCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                                insertCmd.Parameters.Add("parm_ParaDesc", OracleDbType.Varchar2).Value = normalizedParaDesc;
                                insertCmd.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            return new SaveReportParamsResultModel
                            {
                                ParaName = normalizedParaId,
                                Found = false,
                                Inserted = true,
                                Updated = false
                            };
                        }

                        using (var updateCmd = new OracleCommand(updateSql, conn))
                        {
                            updateCmd.BindByName = true;
                            updateCmd.Transaction = transaction;
                            updateCmd.Parameters.Add("parm_ParaDesc", OracleDbType.Varchar2).Value = normalizedParaDesc;
                            updateCmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = normalizedParaId;
                            updateCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return new SaveReportParamsResultModel
                        {
                            ParaName = normalizedParaId,
                            Found = true,
                            Inserted = false,
                            Updated = true
                        };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<ReportListItemModel> GetAllReports()
        {
            const string sql = @"
SELECT repid, paramlist
FROM rep_reports_new
ORDER BY repid";

            var results = new List<ReportListItemModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new ReportListItemModel
                        {
                            RepId = reader["REPID"]?.ToString()?.Trim(),
                            ParamList = reader["PARAMLIST"]?.ToString()?.Trim() ?? string.Empty
                        });
                    }
                }
            }

            return results;
        }

        public PopulateParamTsResultModel PopulateParamTs(string repId, string paramList)
        {
            // Step 1: Load all pending parameters (populated = 0, numeric)
            const string selectPendingParamsSql = @"
SELECT paraname
FROM rep_report_params_new
WHERE populated = 0
  AND TRIM(paraname) IS NOT NULL
ORDER BY paraid";

            // Step 2: Load all reports
            const string selectAllReportsSql = @"
SELECT repid_no, paramlist
FROM rep_reports_new
ORDER BY repid_no";

            // Step 3: Update a single report's paramlist
            const string updateReportSql = @"
UPDATE rep_reports_new
SET paramlist = :param_list
WHERE repid_no = :parm_repid_no";

            // Step 4: Mark all pending params as populated = 1 (numeric)
            const string markParamsPopulatedSql = @"
UPDATE rep_report_params_new
SET populated = 1
WHERE populated = 0";

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // ── Step 1: Fetch pending params (populated = 0 numeric) ──────────
                        var pendingParams = new List<string>();

                        using (var cmd = new OracleCommand(selectPendingParamsSql, conn))
                        {
                            cmd.Transaction = transaction;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var name = reader["PARANAME"]?.ToString()?.Trim();
                                    if (!string.IsNullOrWhiteSpace(name))
                                        pendingParams.Add(name);
                                }
                            }
                        }

                        // Nothing to do — all params already populated
                        if (pendingParams.Count == 0)
                        {
                            transaction.Rollback();
                            return new PopulateParamTsResultModel
                            {
                                UpdatedRows           = 0,
                                Success               = true,
                                ProcessedReports      = 0,
                                ProcessedParams       = 0,
                                AppendedParams        = 0,
                                AlreadyExistingParams = 0
                            };
                        }

                        // ── Step 2: Fetch all reports ─────────────────────────────────────
                        var reports = new List<Tuple<string, string>>(); // (repid_no, paramlist)

                        using (var cmd = new OracleCommand(selectAllReportsSql, conn))
                        {
                            cmd.Transaction = transaction;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    reports.Add(new Tuple<string, string>(
                                        reader["REPID_NO"]?.ToString()?.Trim(),
                                        reader["PARAMLIST"]?.ToString()?.Trim() ?? string.Empty
                                    ));
                                }
                            }
                        }

                        // ── Step 3: Append pending params to each report ──────────────────
                        var totalUpdatedRows     = 0;
                        var totalAppendedParams  = 0;
                        var totalAlreadyExisting = 0;

                        using (var updateCmd = new OracleCommand(updateReportSql, conn))
                        {
                            updateCmd.BindByName  = true;
                            updateCmd.Transaction = transaction;

                            foreach (var report in reports)
                            {
                                var repIdNo          = report.Item1;
                                var currentParamList = report.Item2;

                                // Parse existing param keys (NAME=value format)
                                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                if (!string.IsNullOrWhiteSpace(currentParamList))
                                {
                                    foreach (var token in currentParamList.Split('&'))
                                    {
                                        var t = token?.Trim();
                                        if (string.IsNullOrWhiteSpace(t)) continue;
                                        var eqIdx = t.IndexOf('=');
                                        var key   = eqIdx >= 0 ? t.Substring(0, eqIdx).Trim() : t;
                                        if (!string.IsNullOrWhiteSpace(key))
                                            existingKeys.Add(key);
                                    }
                                }

                                // Determine which pending params are missing
                                var toAppend = new List<string>();
                                foreach (var param in pendingParams)
                                {
                                    if (existingKeys.Contains(param))
                                        totalAlreadyExisting++;
                                    else
                                    {
                                        toAppend.Add(param + "=0");
                                        existingKeys.Add(param);
                                        totalAppendedParams++;
                                    }
                                }

                                if (toAppend.Count == 0)
                                    continue;

                                var newParamList = string.IsNullOrWhiteSpace(currentParamList)
                                    ? string.Join("&", toAppend)
                                    : currentParamList.TrimEnd('&') + "&" + string.Join("&", toAppend);

                                updateCmd.Parameters.Clear();
                                updateCmd.Parameters.Add("param_list",    OracleDbType.Varchar2).Value = newParamList;
                                updateCmd.Parameters.Add("parm_repid_no", OracleDbType.Varchar2).Value = repIdNo;

                                totalUpdatedRows += updateCmd.ExecuteNonQuery();
                            }
                        }

                        // ── Step 4: Mark pending params as populated = 1 ──────────────────
                        using (var markCmd = new OracleCommand(markParamsPopulatedSql, conn))
                        {
                            markCmd.Transaction = transaction;
                            markCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return new PopulateParamTsResultModel
                        {
                            UpdatedRows           = totalUpdatedRows,
                            Success               = true,
                            ProcessedReports      = reports.Count,
                            ProcessedParams       = pendingParams.Count,
                            AppendedParams        = totalAppendedParams,
                            AlreadyExistingParams = totalAlreadyExisting
                        };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<ReportParameterListItemModel> GetPopedRepParams()
        {
            const string sql = @"
SELECT paraid, paraname, description
FROM rep_report_params_new
WHERE populated = 1
ORDER BY paraid";

            var results = new List<ReportParameterListItemModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new ReportParameterListItemModel
                        {
                            ParaId = reader["PARAID"]?.ToString()?.Trim(),
                            ParaName = reader["PARANAME"]?.ToString()?.Trim(),
                            Description = reader["DESCRIPTION"]?.ToString()?.Trim(),
                            Populated = true
                        });
                    }
                }
            }

            return results;
        }

        public DeleteReportParamResultModel DeleteReportParam(string paraId)
        {
            const string deleteSql = @"
DELETE FROM rep_report_params_new
WHERE UPPER(TRIM(paraname)) = :parm_ParaID";

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new OracleCommand(deleteSql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.Transaction = transaction;
                            cmd.Parameters.Add("parm_ParaID", OracleDbType.Varchar2).Value = paraId?.Trim()?.ToUpperInvariant();

                            var deletedRows = cmd.ExecuteNonQuery();
                            transaction.Commit();

                            return new DeleteReportParamResultModel
                            {
                                DeletedRows = deletedRows
                            };
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// DEBUG: Returns the raw Oracle value + DUMP() of the populated column for each row.
        /// This reveals the actual data type (NUMBER vs VARCHAR2) stored in the column.
        /// Remove once the production issue is fixed.
        /// </summary>
        public List<object> GetRawPopulatedValues()
        {
            const string sql = @"
SELECT paraname,
       TO_CHAR(populated)            AS populated_char,
       DUMP(populated)               AS populated_dump,
       CASE WHEN NVL(TO_CHAR(populated),'0') IN ('0','No','no','NO') THEN 'PENDING' ELSE 'DONE' END AS status
FROM rep_report_params_new
ORDER BY paraname";

            var results = new List<object>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new
                        {
                            paraName      = reader["PARANAME"]?.ToString(),
                            populatedChar = reader["POPULATED_CHAR"]?.ToString(),
                            populatedDump = reader["POPULATED_DUMP"]?.ToString(),
                            status        = reader["STATUS"]?.ToString()
                        });
                    }
                }
            }

            return results;
        }
    }
} 