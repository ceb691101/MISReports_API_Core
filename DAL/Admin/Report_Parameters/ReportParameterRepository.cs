using System;
using System.Collections.Generic;
using System.Configuration;
using MISReports_Api.Models.Admin.Report_Parameters;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.DAL.Admin.Report_Parameters
{
    public class ReportParameterRepository : IReportParameterRepository
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ParameterItemModel> GetParameters()
        {
            const string sql = @"
SELECT TRIM(paraname) AS paraname,
       TRIM(description) AS description
FROM rep_report_params_new
WHERE TRIM(paraname) IS NOT NULL
ORDER BY UPPER(TRIM(paraname))";

            var rows = new List<ParameterItemModel>();

            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new ParameterItemModel
                        {
                            Name = reader["PARANAME"]?.ToString()?.Trim(),
                            Description = reader["DESCRIPTION"]?.ToString()?.Trim()
                        });
                    }
                }
            }

            return rows;
        }

        public ParameterUpsertResultModel UpsertParameter(string name, string description)
        {
            const string existsSql = @"
SELECT COUNT(1)
FROM rep_report_params_new
WHERE UPPER(TRIM(paraname)) = :paraname";

            const string insertSql = @"
INSERT INTO rep_report_params_new (paraid, paraname, description, populated)
VALUES (0, :paraname, :description, 0)";

            const string updateSql = @"
UPDATE rep_report_params_new
SET description = :description
WHERE UPPER(TRIM(paraname)) = :paraname";

            var normalizedName = name?.Trim().ToUpperInvariant();
            var cleanName = name?.Trim();
            var cleanDescription = description?.Trim();

            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var exists = false;
                        using (var existsCmd = new OracleCommand(existsSql, conn))
                        {
                            existsCmd.BindByName = true;
                            existsCmd.Transaction = tx;
                            existsCmd.Parameters.Add("paraname", OracleDbType.Varchar2).Value = normalizedName;
                            var countObj = existsCmd.ExecuteScalar();
                            var count = countObj == null || countObj == DBNull.Value ? 0 : Convert.ToInt32(countObj);
                            exists = count > 0;
                        }

                        if (exists)
                        {
                            using (var updateCmd = new OracleCommand(updateSql, conn))
                            {
                                updateCmd.BindByName = true;
                                updateCmd.Transaction = tx;
                                updateCmd.Parameters.Add("description", OracleDbType.Varchar2).Value = cleanDescription;
                                updateCmd.Parameters.Add("paraname", OracleDbType.Varchar2).Value = normalizedName;
                                updateCmd.ExecuteNonQuery();
                            }

                            tx.Commit();
                            return new ParameterUpsertResultModel
                            {
                                Name = cleanName,
                                Updated = true,
                                Inserted = false
                            };
                        }

                        using (var insertCmd = new OracleCommand(insertSql, conn))
                        {
                            insertCmd.BindByName = true;
                            insertCmd.Transaction = tx;
                            insertCmd.Parameters.Add("paraname", OracleDbType.Varchar2).Value = cleanName;
                            insertCmd.Parameters.Add("description", OracleDbType.Varchar2).Value = cleanDescription;
                            insertCmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return new ParameterUpsertResultModel
                        {
                            Name = cleanName,
                            Updated = false,
                            Inserted = true
                        };
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public int DeleteParameter(string name)
        {
            const string deleteChildSql = @"
DELETE FROM report_parameters
WHERE UPPER(TRIM(paraname)) = :paraname";

            const string deleteMasterSql = @"
DELETE FROM rep_report_params_new
WHERE UPPER(TRIM(paraname)) = :paraname";

            var normalizedName = name?.Trim().ToUpperInvariant();

            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var deleteChildCmd = new OracleCommand(deleteChildSql, conn))
                        {
                            deleteChildCmd.BindByName = true;
                            deleteChildCmd.Transaction = tx;
                            deleteChildCmd.Parameters.Add("paraname", OracleDbType.Varchar2).Value = normalizedName;
                            deleteChildCmd.ExecuteNonQuery();
                        }

                        int deleted;
                        using (var deleteMasterCmd = new OracleCommand(deleteMasterSql, conn))
                        {
                            deleteMasterCmd.BindByName = true;
                            deleteMasterCmd.Transaction = tx;
                            deleteMasterCmd.Parameters.Add("paraname", OracleDbType.Varchar2).Value = normalizedName;
                            deleted = deleteMasterCmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return deleted;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public List<ReportItemModel> GetReports()
        {
            const string sql = @"
SELECT r.repid,
       (SELECT COUNT(1)
        FROM report_parameters rp
        WHERE UPPER(TRIM(rp.repid)) = UPPER(TRIM(r.repid))) AS parameter_count
FROM rep_reports_new r
ORDER BY UPPER(TRIM(r.repid))";

            var rows = new List<ReportItemModel>();

            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new ReportItemModel
                        {
                            RepId = reader["REPID"]?.ToString()?.Trim(),
                            ParameterCount = reader["PARAMETER_COUNT"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PARAMETER_COUNT"])
                        });
                    }
                }
            }

            return rows;
        }

        public PopulateResultModel PopulateMissingParameters()
        {
            const string reportsCountSql = @"
SELECT COUNT(1)
FROM rep_reports_new";

            const string paramsCountSql = @"
SELECT COUNT(1)
FROM rep_report_params_new
WHERE TRIM(paraname) IS NOT NULL";

            const string populateSql = @"
INSERT INTO report_parameters (repid, paraname, value)
SELECT r.repid,
       p.paraname,
       '0'
FROM rep_reports_new r
CROSS JOIN rep_report_params_new p
WHERE TRIM(p.paraname) IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM report_parameters rp
      WHERE UPPER(TRIM(rp.repid)) = UPPER(TRIM(r.repid))
        AND UPPER(TRIM(rp.paraname)) = UPPER(TRIM(p.paraname))
  )";

            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable))
                {
                    try
                    {
                        var reportsCount = 0;
                        using (var reportsCmd = new OracleCommand(reportsCountSql, conn))
                        {
                            reportsCmd.BindByName = true;
                            reportsCmd.Transaction = tx;
                            var value = reportsCmd.ExecuteScalar();
                            reportsCount = value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
                        }

                        var paramsCount = 0;
                        using (var paramsCmd = new OracleCommand(paramsCountSql, conn))
                        {
                            paramsCmd.BindByName = true;
                            paramsCmd.Transaction = tx;
                            var value = paramsCmd.ExecuteScalar();
                            paramsCount = value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
                        }

                        var insertedRows = 0;
                        using (var populateCmd = new OracleCommand(populateSql, conn))
                        {
                            populateCmd.BindByName = true;
                            populateCmd.Transaction = tx;
                            insertedRows = populateCmd.ExecuteNonQuery();
                        }

                        tx.Commit();

                        var totalCombinations = reportsCount * paramsCount;
                        var alreadyExisting = totalCombinations - insertedRows;
                        if (alreadyExisting < 0)
                        {
                            alreadyExisting = 0;
                        }

                        return new PopulateResultModel
                        {
                            ReportsCount = reportsCount,
                            ParametersCount = paramsCount,
                            InsertedRows = insertedRows,
                            AlreadyExistingRows = alreadyExisting
                        };
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}
