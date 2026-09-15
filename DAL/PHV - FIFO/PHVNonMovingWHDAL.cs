using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class PHVNonMovingWHDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PHVNonMovingWHModel> GetPHVNonMovingWH(string repYear, string whCode)
        {
            var result = new List<PHVNonMovingWHModel>();
            const string query = @"
                SELECT F.doc_no, F.mat_cd, M.mat_nm, F.grade_cd, F.doc_dt AS phv_dt,
                       SUM(CASE WHEN F.damage_count > 0 THEN F.damage_count ELSE F.counted_qty END) AS qty_on_hand,
                       SUM(F.unit_price * (CASE WHEN F.damage_count > 0 THEN F.damage_count ELSE F.counted_qty END)) AS stockbook,
                       F.reason,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = SUBSTR(F.doc_no,0,6)) AS CCT_NAME
                FROM fifo_phv F, inmatm M
                WHERE TRIM(F.mat_cd) = TRIM(M.mat_cd)
                  AND F.moving_status = 2
                  AND F.dept_id = SUBSTR(F.doc_no,0,6)
                  AND F.status = :repyear
                  AND F.wrh_cd = :whcode
                GROUP BY F.doc_no, F.mat_cd, M.mat_nm, F.grade_cd, F.reason, F.doc_dt
                ORDER BY F.doc_no, F.mat_cd";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Varchar2) { Value = repYear });
                cmd.Parameters.Add(new OracleParameter("whcode", OracleDbType.Varchar2) { Value = whCode });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new PHVNonMovingWHModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            MatNm = reader["mat_nm"] == DBNull.Value ? null : reader["mat_nm"].ToString(),
                            GradeCd = reader["grade_cd"] == DBNull.Value ? null : reader["grade_cd"].ToString(),
                            PhvDt = reader["phv_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["phv_dt"]),
                            QtyOnHand = reader["qty_on_hand"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["qty_on_hand"]),
                            StockBook = reader["stockbook"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["stockbook"]),
                            Reason = reader["reason"] == DBNull.Value ? null : reader["reason"].ToString(),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}