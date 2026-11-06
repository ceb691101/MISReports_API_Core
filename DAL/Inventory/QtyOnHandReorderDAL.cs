using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class QtyOnHandReorderDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<QtyOnHandReorderModel> GetQtyOnHandReorder(string costCtr)
        {
            var result = new List<QtyOnHandReorderModel>();

            string query = @"
        SELECT T3.wrh_cd, T1.mat_cd, T2.mat_nm, T3.qty_on_hand, T1.reord_qty, T3.uom_cd, T1.ref_1,
               T1.min_stock, T1.bldg_no, T1.column_no, T1.shelf_no, T1.bin_no,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
        FROM inwhmtdm T1, inwrhmtm T3, inmatm T2
        WHERE T1.dept_id = T3.dept_id
          AND T1.wrh_cd = T3.wrh_cd
          AND T1.mat_cd = T3.mat_cd
          AND T1.grade_cd = T3.grade_cd
          AND T2.mat_cd = T3.mat_cd
          AND T3.status = 2
          AND T1.dept_id = :costctr
        GROUP BY T3.wrh_cd, T1.mat_cd, T2.mat_nm, T3.qty_on_hand, T1.reord_qty, T3.uom_cd, T1.ref_1,
                 T1.min_stock, T1.bldg_no, T1.column_no, T1.shelf_no, T1.bin_no
        ORDER BY T3.wrh_cd, 7, 1 ASC, 2 ASC, 3 ASC";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new QtyOnHandReorderModel
                        {
                            WrhCd = reader["wrh_cd"] == DBNull.Value ? null : reader["wrh_cd"].ToString(),
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            MatNm = reader["mat_nm"] == DBNull.Value ? null : reader["mat_nm"].ToString(),
                            QtyOnHand = reader["qty_on_hand"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["qty_on_hand"]),
                            ReordQty = reader["reord_qty"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["reord_qty"]),
                            UomCd = reader["uom_cd"] == DBNull.Value ? null : reader["uom_cd"].ToString(),
                            Ref1 = reader["ref_1"] == DBNull.Value ? null : reader["ref_1"].ToString(),
                            MinStock = reader["min_stock"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["min_stock"]),
                            BldgNo = reader["bldg_no"] == DBNull.Value ? null : reader["bldg_no"].ToString(),
                            ColumnNo = reader["column_no"] == DBNull.Value ? null : reader["column_no"].ToString(),
                            ShelfNo = reader["shelf_no"] == DBNull.Value ? null : reader["shelf_no"].ToString(),
                            BinNo = reader["bin_no"] == DBNull.Value ? null : reader["bin_no"].ToString(),
                            BranchName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}