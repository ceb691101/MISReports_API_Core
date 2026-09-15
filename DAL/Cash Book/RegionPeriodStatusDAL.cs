using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class RegionPeriodStatusDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        public List<RegionPeriodStatusModel> GetRegionPeriodStatus(string repYear, string repMonth, string region)
        {
            var result = new List<RegionPeriodStatusModel>();
            const string query = @"
                SELECT T1.dept_id,
                       T2.dept_nm,
                       T2.comp_id,
                       T1.fin_year,
                       T1.fin_prd,
                       (CASE WHEN T1.prd_stat = 1 THEN 'future period'
                             WHEN T1.prd_stat = 2 THEN 'Open period'
                             WHEN T1.prd_stat = 3 THEN 'Current period'
                             WHEN T1.prd_stat = 4 THEN 'Soft closed period'
                             WHEN T1.prd_stat = 5 THEN 'Hard closed period'
                             ELSE T1.prd_stat || ' -  Unknown' END) AS status,
                       (SELECT comp_nm FROM glcompm WHERE comp_id = :region) AS comp_nm
                FROM GLFNPRDM T1, gldeptm T2
                WHERE T1.fin_year = :repyear
                  AND T1.dept_id = T2.dept_id
                  AND T1.dept_id IN (
                        SELECT dept_id FROM gldeptm
                        WHERE comp_id IN (
                            SELECT comp_id FROM glcompm
                            WHERE comp_id = :region
                               OR parent_id = :region
                               OR grp_comp = :region))
                  AND T1.fin_prd = :repmonth
                ORDER BY T1.dept_id";
            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :region (used 4x) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("region", OracleDbType.Varchar2) { Value = region });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Varchar2) { Value = repYear });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Varchar2) { Value = repMonth });
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new RegionPeriodStatusModel
                        {
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            DeptNm = reader["dept_nm"] == DBNull.Value ? null : reader["dept_nm"].ToString(),
                            CompId = reader["comp_id"] == DBNull.Value ? null : reader["comp_id"].ToString(),
                            FinYear = reader["fin_year"] == DBNull.Value ? null : reader["fin_year"].ToString(),
                            FinPrd = reader["fin_prd"] == DBNull.Value ? null : reader["fin_prd"].ToString(),
                            Status = reader["status"] == DBNull.Value ? null : reader["status"].ToString(),
                            CompNm = reader["comp_nm"] == DBNull.Value ? null : reader["comp_nm"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}