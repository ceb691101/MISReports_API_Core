using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Inventory;

namespace MISReports_Api.DAL.Inventory
{
    public class QuantityOnHandLessThanReorderLevelDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<QuantityOnHandLessThanReorderLevelModel>
            GetQuantityOnHandLessThanReorderLevel(string costCtr)
        {
            var result =
                new List<QuantityOnHandLessThanReorderLevelModel>();

            const string query = @"
                SELECT
                    T3.wrh_cd,
                    T1.mat_cd,
                    T2.mat_nm,
                    T3.qty_on_hand,
                    T1.reord_qty,
                    T3.uom_cd,
                    T1.ref_1,
                    T1.min_stock,
                    (
                        SELECT dept_nm
                        FROM gldeptm
                        WHERE dept_id = :costctr
                    ) AS cct_name
                FROM inwhmtdm T1,
                     inwrhmtm T3,
                     inmatm T2
                WHERE T1.dept_id = T3.dept_id
                  AND T1.wrh_cd = T3.wrh_cd
                  AND T1.mat_cd = T3.mat_cd
                  AND T1.grade_cd = T3.grade_cd
                  AND T2.mat_cd = T3.mat_cd
                  AND T3.qty_on_hand < T1.reord_qty
                  AND T3.status = 2
                  AND T1.dept_id = :costctr
                GROUP BY
                    T3.wrh_cd,
                    T1.mat_cd,
                    T2.mat_nm,
                    T3.qty_on_hand,
                    T1.reord_qty,
                    T3.uom_cd,
                    T1.ref_1,
                    T1.min_stock
                ORDER BY
                    T3.wrh_cd,
                    T1.ref_1,
                    T1.mat_cd,
                    T2.mat_nm";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(
                    new OracleParameter("costctr", OracleDbType.Varchar2)
                    {
                        Value = costCtr
                    });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(
                            new QuantityOnHandLessThanReorderLevelModel
                            {
                                WarehouseCode =
                                    reader["wrh_cd"] == DBNull.Value
                                        ? null
                                        : reader["wrh_cd"].ToString(),

                                MaterialCode =
                                    reader["mat_cd"] == DBNull.Value
                                        ? null
                                        : reader["mat_cd"].ToString(),

                                MaterialName =
                                    reader["mat_nm"] == DBNull.Value
                                        ? null
                                        : reader["mat_nm"].ToString(),

                                QuantityOnHand =
                                    reader["qty_on_hand"] == DBNull.Value
                                        ? (decimal?)null
                                        : Convert.ToDecimal(
                                            reader["qty_on_hand"]),

                                ReorderQuantity =
                                    reader["reord_qty"] == DBNull.Value
                                        ? (decimal?)null
                                        : Convert.ToDecimal(
                                            reader["reord_qty"]),

                                UomCode =
                                    reader["uom_cd"] == DBNull.Value
                                        ? null
                                        : reader["uom_cd"].ToString(),

                                Reference =
                                    reader["ref_1"] == DBNull.Value
                                        ? null
                                        : reader["ref_1"].ToString(),

                                MinimumStock =
                                    reader["min_stock"] == DBNull.Value
                                        ? (decimal?)null
                                        : Convert.ToDecimal(
                                            reader["min_stock"]),

                                CostCentreName =
                                    reader["cct_name"] == DBNull.Value
                                        ? null
                                        : reader["cct_name"].ToString()
                            });
                    }
                }
            }

            return result;
        }
    }
}