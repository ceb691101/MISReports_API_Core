using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class CurrentAcctBalCCDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CurrentAcctBalCCModel> GetCurrentAcctBalCC(string repYear, string repMonth, string costCtr)
        {
            var result = new List<CurrentAcctBalCCModel>();

            const string query = @"
                SELECT  A.sub_ac,
                        B.ac_nm,
                        A.cl_bal,
                        (SELECT comp_nm
                           FROM glcompm, gldeptm
                          WHERE TRIM(gldeptm.dept_id) = TRIM(:costctr)
                            AND gldeptm.comp_id = glcompm.comp_id) AS cct_name
                FROM      glsubbal A, glsubacm B
                WHERE     TRIM(A.gl_cd) = TRIM(:glcd)
                  AND A.yr_ind = :repyear
                  AND A.mth_ind = :repmonth
                  AND A.sub_ac = B.sub_ac
                  AND A.gl_cd = B.gl_cd";

            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string glCd = costCtrTrimmed + "-L9100";

            int repYearNum = int.Parse((repYear ?? string.Empty).Trim());
            int repMonthNum = int.Parse((repMonth ?? string.Empty).Trim());

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("glcd", OracleDbType.Varchar2) { Value = glCd });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Int32) { Value = repYearNum });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Int32) { Value = repMonthNum });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CurrentAcctBalCCModel
                        {
                            SubAc = reader["sub_ac"] == DBNull.Value ? null : reader["sub_ac"].ToString(),
                            AcNm = reader["ac_nm"] == DBNull.Value ? null : reader["ac_nm"].ToString(),
                            ClBal = reader["cl_bal"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["cl_bal"]),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        public object GetBoundParamDebug(string repYear, string repMonth, string costCtr)
        {
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string glCd = costCtrTrimmed + "-L9100";
            int repYearNum = int.Parse((repYear ?? string.Empty).Trim());
            int repMonthNum = int.Parse((repMonth ?? string.Empty).Trim());

            const string diagQuery = @"
            SELECT
                :costctr        AS bound_costctr,
                :glcd           AS bound_glcd,
                :repyear        AS bound_repyear,
                :repmonth       AS bound_repmonth,
                SYS_CONTEXT('USERENV','DB_NAME') AS db_name,
                SYS_CONTEXT('USERENV','CURRENT_SCHEMA') AS schema_name,
                (SELECT COUNT(*) FROM glsubbal WHERE TRIM(gl_cd) = TRIM(:glcd2)) AS matches_glcd_only,
                (SELECT COUNT(*) FROM glsubbal WHERE yr_ind = :repyear2 AND mth_ind = :repmonth2) AS matches_year_month_only,
                (SELECT COUNT(*) FROM glsubbal 
                  WHERE TRIM(gl_cd) = TRIM(:glcd3) AND yr_ind = :repyear3 AND mth_ind = :repmonth3) AS matches_all_three
            FROM DUAL";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(diagQuery, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("glcd", OracleDbType.Varchar2) { Value = glCd });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Int32) { Value = repYearNum });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Int32) { Value = repMonthNum });
                cmd.Parameters.Add(new OracleParameter("glcd2", OracleDbType.Varchar2) { Value = glCd });
                cmd.Parameters.Add(new OracleParameter("repyear2", OracleDbType.Int32) { Value = repYearNum });
                cmd.Parameters.Add(new OracleParameter("repmonth2", OracleDbType.Int32) { Value = repMonthNum });
                cmd.Parameters.Add(new OracleParameter("glcd3", OracleDbType.Varchar2) { Value = glCd });
                cmd.Parameters.Add(new OracleParameter("repyear3", OracleDbType.Int32) { Value = repYearNum });
                cmd.Parameters.Add(new OracleParameter("repmonth3", OracleDbType.Int32) { Value = repMonthNum });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new
                        {
                            boundCostCtr = reader["bound_costctr"]?.ToString(),
                            boundGlCd = reader["bound_glcd"]?.ToString(),
                            boundRepYear = reader["bound_repyear"]?.ToString(),
                            boundRepMonth = reader["bound_repmonth"]?.ToString(),
                            dbName = reader["db_name"]?.ToString(),
                            schemaName = reader["schema_name"]?.ToString(),
                            matchesGlCdOnly = reader["matches_glcd_only"]?.ToString(),
                            matchesYearMonthOnly = reader["matches_year_month_only"]?.ToString(),
                            matchesAllThree = reader["matches_all_three"]?.ToString()
                        };
                    }
                }
            }
            return null;
        }
    }
}