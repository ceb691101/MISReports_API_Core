using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class QuantityMatFIFODAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<QuantityMatFIFOModel> GetQuantityMatFIFO(string costCtr, string whCode, string matCode)
        {
            var result = new List<QuantityMatFIFOModel>();

            const string query = @"
                SELECT  B.wrh_cd,
                        B.mat_cd,
                        D.mat_nm,
                        A.grade_cd AS grd_cd,
                        D.maj_uom,
                        SUM(B.QTY - B.ALLOCATED_qty) AS qty_on_hand,
                        B.unit_price,
                        SUM(B.unit_price * (B.QTY - B.ALLOCATED_qty)) AS value,
                        (B.dept_id || ' - ' || (SELECT dept_nm
                                                   FROM gldeptm
                                                  WHERE dept_id = B.dept_id)) AS cct_name
                FROM      inwrhmtm A, inmatm D, inpodmt B
                WHERE     A.mat_cd LIKE :matcode || '%'
                  AND     A.status IN (2, 7)
                  AND     B.status IN (4, 7)
                  AND     A.dept_id = B.dept_id
                  AND     A.mat_cd = D.mat_cd
                  AND     A.mat_cd = B.mat_cd
                  AND     TRIM(A.wrh_cd) = TRIM(B.wrh_cd)
                  AND     B.QTY > 0
                  AND     UPPER(TRIM(B.wrh_cd)) = UPPER(:whcode)
                  AND     TRIM(B.dept_id) = :costctr
                GROUP BY B.dept_id, B.mat_cd, D.mat_nm, A.grade_cd, D.maj_uom, B.unit_price, B.wrh_cd
                ORDER BY B.wrh_cd, B.mat_cd";

            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string whCodeTrimmed = (whCode ?? string.Empty).Trim();
            string matCodeTrimmed = (matCode ?? string.Empty).Trim();

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2) { Value = matCodeTrimmed });
                cmd.Parameters.Add(new OracleParameter("whcode", OracleDbType.Varchar2) { Value = whCodeTrimmed });
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new QuantityMatFIFOModel
                        {
                            WrhCd = reader["wrh_cd"] == DBNull.Value ? null : reader["wrh_cd"].ToString(),
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            MatNm = reader["mat_nm"] == DBNull.Value ? null : reader["mat_nm"].ToString(),
                            GrdCd = reader["grd_cd"] == DBNull.Value ? null : reader["grd_cd"].ToString(),
                            MajUom = reader["maj_uom"] == DBNull.Value ? null : reader["maj_uom"].ToString(),
                            QtyOnHand = reader["qty_on_hand"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["qty_on_hand"]),
                            UnitPrice = reader["unit_price"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["unit_price"]),
                            Value = reader["value"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["value"]),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}