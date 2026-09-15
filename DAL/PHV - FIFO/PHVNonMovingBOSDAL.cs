using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class PHVNonMovingBOSDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PHVNonMovingBOSModel> GetPHVNonMovingBOS(string repYear, string whCode)
        {
            var result = new List<PHVNonMovingBOSModel>();
            const string query = @"
                SELECT F.doc_no, F.mat_cd, M.mat_nm, F.grade_cd, F.damage_count, F.batch_id,
                       (CASE WHEN F.damage_count > 0 THEN F.damage_count ELSE F.counted_qty END) AS qty_on_hand,
                       F.unit_price,
                       (F.unit_price * F.damage_count) AS stockbook,
                       F.reason,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = SUBSTR(F.doc_no,0,6)) AS CCT_NAME
                FROM fifo_phv F, inmatm M
                WHERE TRIM(F.mat_cd) = TRIM(M.mat_cd)
                  AND F.moving_status = 3
                  AND F.dept_id = SUBSTR(F.doc_no,0,6)
                  AND F.status = :repyear
                  AND F.wrh_cd = :whcode
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
                        result.Add(new PHVNonMovingBOSModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            MatNm = reader["mat_nm"] == DBNull.Value ? null : reader["mat_nm"].ToString(),
                            GradeCd = reader["grade_cd"] == DBNull.Value ? null : reader["grade_cd"].ToString(),
                            DamageCount = reader["damage_count"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["damage_count"]),
                            BatchId = reader["batch_id"] == DBNull.Value ? null : reader["batch_id"].ToString(),
                            QtyOnHand = reader["qty_on_hand"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["qty_on_hand"]),
                            UnitPrice = reader["unit_price"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["unit_price"]),
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