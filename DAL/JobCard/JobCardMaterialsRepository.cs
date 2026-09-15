using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class JobCardMaterialsRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<JobCardMaterialsModel>> GetMaterialConsumptionAsync(string projectNo, string costCenter)
        {
            var result = new List<JobCardMaterialsModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"SELECT
    T1.trx_type,
    T2.doc_no,
    T1.trx_dt,
    T1.mat_cd,
    (SELECT mat_nm
       FROM inmatm
      WHERE mat_cd = T1.mat_cd) AS mat_nm,
    T1.grade_cd,
    T1.trx_qty,
    T1.unit_cost,
    (CASE WHEN T1.add_deduct = 'F'
          THEN T1.trx_val
          ELSE - T1.trx_val
     END) AS trx_val
FROM inpostmt T1
JOIN intrhmt T2
  ON T1.doc_no = T2.doc_no
 AND T1.doc_pf = T2.doc_pf
 AND T1.dept_id = T2.dept_id
WHERE TRIM(T2.dept_id) =:costctr
  AND(T2.issue_to IN(1, 5)
       OR T2.rc_from IN(1, 5))
  AND T1.trx_type IN('ISSUE     ', 'IS_CAN    ', 'RC_CAN    ', 'RECEIPT   ')
  AND TRIM(
        CASE
          WHEN T2.is_ref IS NOT NULL THEN T2.is_ref
          WHEN T2.rc_ref IS NOT NULL THEN T2.rc_ref
          ELSE NULL
        END
      ) = :project_no
GROUP BY
    T1.trx_type,
    T2.doc_no,
    T1.trx_dt,
    T1.mat_cd,
    T1.grade_cd,
    T1.trx_qty,
    T1.unit_cost,
    (CASE WHEN T1.add_deduct = 'F'
          THEN T1.trx_val
          ELSE - T1.trx_val
     END)
ORDER BY
    T1.trx_type ASC,
    T2.doc_no ASC";
                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("costctr", costCenter);
                        cmd.Parameters.Add("project_no", projectNo);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new JobCardMaterialsModel
                                {
                                    TrxType = reader["trx_type"]?.ToString()?.Trim(),
                                    DocNo = reader["doc_no"]?.ToString()?.Trim(),
                                    TrxDt = reader["trx_dt"] != DBNull.Value ? Convert.ToDateTime(reader["trx_dt"]) : (DateTime?)null,
                                    MatCd = reader["mat_cd"]?.ToString()?.Trim(),
                                    MatNm = reader["mat_nm"] != DBNull.Value ? reader["mat_nm"].ToString().Trim() : null,
                                    GradeCd = reader["grade_cd"]?.ToString()?.Trim(),
                                    TrxQty = reader["trx_qty"] != DBNull.Value ? Convert.ToDecimal(reader["trx_qty"]) : 0m,
                                    UnitCost = reader["unit_cost"] != DBNull.Value ? Convert.ToDecimal(reader["unit_cost"]) : 0m,
                                    TrxVal = reader["trx_val"] != DBNull.Value ? Convert.ToDecimal(reader["trx_val"]) : 0m
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMaterialConsumption: {ex.Message}\n{ex.StackTrace}");
                throw;
            }

            return result;
        }
    }
}