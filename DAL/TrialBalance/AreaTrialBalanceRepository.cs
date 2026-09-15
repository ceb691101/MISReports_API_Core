using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class AreaTrialBalanceRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<AreaTrialBalanceModel> GetAreaTrialBalanceData(string compId, int repyear, int repmonth)
        {
            var results = new List<AreaTrialBalanceModel>();

            try
            {
                compId = compId?.Trim().ToUpper();

                using (var connection = new OracleConnection(connectionString))
                {
                    connection.Open();

                    if (connection.State != ConnectionState.Open)
                    {
                        throw new Exception("Failed to open database connection");
                    }

                    string sql = @"
                    SELECT 
                        glledgrm.ac_cd, 
                        glledgrm.gl_nm,
                        CASE 
                            WHEN substr(glledgrm.ac_cd, 1, 1) IN ('A') THEN 'A' 
                            WHEN substr(glledgrm.ac_cd, 1, 1) IN ('E') THEN 'E'
                            WHEN substr(glledgrm.ac_cd, 1, 1) IN ('L') THEN 'L' 
                            ELSE 'R' 
                        END as titile_flag,
                        ROUND(SUM(gllegbal.op_bal), 2) AS op_sbal,
                        ROUND(SUM(gllegbal.dr_amt), 2) AS dr_samt, 
                        ROUND(SUM(gllegbal.cr_amt), 2) AS cr_samt, 
                        ROUND(SUM(gllegbal.cl_bal), 2) AS cl_sbal,
                        (SELECT comp_nm FROM glcompm WHERE comp_id = :compId) as cct_name
                    FROM 
                        gllegbal, 
                        glledgrm, 
                        glacgrpm, 
                        gltitlm
                    WHERE 
                        glledgrm.gl_cd = gllegbal.gl_cd
                        AND glledgrm.ac_cd = glacgrpm.ac_cd
                        AND glacgrpm.title_cd = gltitlm.title_cd
                        AND gllegbal.dept_id IN (
                            SELECT dept_id FROM gldeptm
                            WHERE comp_id IN (
                                SELECT comp_id FROM glcompm
                                WHERE comp_id = :compId OR parent_id = :compId
                            )
                        )
                        AND gllegbal.yr_ind = :repyear
                        AND gllegbal.mth_ind = :repmonth
                        AND gltitlm.title_cd LIKE 'TB%'
                    GROUP BY 
                        glledgrm.ac_cd, 
                        glledgrm.gl_nm
                    ORDER BY 
                        glledgrm.ac_cd";

                    using (var command = new OracleCommand(sql, connection))
                    {
                        command.BindByName = true;
                        
                        command.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId;
                        command.Parameters.Add("repyear", OracleDbType.Int32).Value = repyear;
                        command.Parameters.Add("repmonth", OracleDbType.Int32).Value = repmonth;

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(new AreaTrialBalanceModel
                                {
                                    AccountCode = reader["ac_cd"].ToString(),
                                    AccountName = reader["gl_nm"].ToString(),
                                    TitleFlag = reader["titile_flag"].ToString(),
                                    OpeningBalance = SafeGetDecimal(reader["op_sbal"]),
                                    DebitAmount = SafeGetDecimal(reader["dr_samt"]),
                                    CreditAmount = SafeGetDecimal(reader["cr_samt"]),
                                    ClosingBalance = SafeGetDecimal(reader["cl_sbal"]),
                                    CompanyName = reader["cct_name"]?.ToString() ?? string.Empty
                                });
                            }
                        }
                    }
                }
                return results;
            }
            catch (OracleException ex)
            {
                throw new Exception($"Oracle Error {ex.Number}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database operation failed: {ex.Message}", ex);
            }
        }

        private decimal SafeGetDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0m;
            return Convert.ToDecimal(value);
        }
    }
}
