using MISReports_Api.Compat;
using MISReports_Api.Models.Inventory;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;

namespace MISReports_Api.DAL.Inventory
{
    public class QuantityOnHandGradeCodewiseDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<QuantityOnHandGradeCodewiseModel> GetQuantityOnHandGradeCodewise(
            string costCenter,
            string matCode,
            string gradeCode)
        {
            var result = new List<QuantityOnHandGradeCodewiseModel>();

            const string query = @"
                SELECT
                    A.wrh_cd,
                    A.mat_cd AS MAT_CD,
                    D.mat_nm AS MAT_NM,
                    A.grade_cd AS GRD_CD,
                    D.maj_uom AS MAJ_UOM,
                    SUM(A.qty_on_hand) AS QTY_ON_HAND,
                    A.unit_price AS UNIT_PRICE,
                    SUM(A.unit_price * A.qty_on_hand) AS VALUE,
                    CASE
                        WHEN A.status = 2 THEN 'Active'
                        WHEN A.status = 3 THEN 'Inactive'
                        WHEN A.status = 7 THEN 'Frezze'
                        ELSE 'Unknowm - ' || A.status
                    END AS STATUS,
                    (
                        SELECT dept_nm
                        FROM gldeptm
                        WHERE dept_id = :costctr
                    ) AS CCT_NAME
                FROM inwrhmtm A
                INNER JOIN inmatm D
                    ON A.mat_cd = D.mat_cd
                WHERE A.dept_id = :costctr
                    AND A.mat_cd LIKE :matcode || '%'
                    AND A.grade_cd = :grcode
                GROUP BY
                    A.wrh_cd,
                    A.mat_cd,
                    D.mat_nm,
                    A.grade_cd,
                    D.maj_uom,
                    A.qty_on_hand,
                    A.unit_price,
                    A.mat_cost,
                    A.status
                ORDER BY
                    A.wrh_cd,
                    A.mat_cd";

            using (var connection = new OracleConnection(_connectionString))
            using (var command = new OracleCommand(query, connection))
            {
                command.CommandType = CommandType.Text;
                command.BindByName = true;

                command.Parameters.Add(
                    "costctr",
                    OracleDbType.Varchar2
                ).Value = costCenter;

                command.Parameters.Add(
                    "matcode",
                    OracleDbType.Varchar2
                ).Value = matCode ?? string.Empty;

                command.Parameters.Add(
                    "grcode",
                    OracleDbType.Varchar2
                ).Value = gradeCode;

                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new QuantityOnHandGradeCodewiseModel
                        {
                            WarehouseCode = reader["WRH_CD"] == DBNull.Value
                                ? null
                                : Convert.ToString(reader["WRH_CD"]),

                            MaterialCode = reader["MAT_CD"] == DBNull.Value
                                ? null
                                : Convert.ToString(reader["MAT_CD"]),

                            MaterialName = reader["MAT_NM"] == DBNull.Value
                                ? null
                                : Convert.ToString(reader["MAT_NM"]),

                            GradeCode = reader["GRD_CD"] == DBNull.Value
                                ? null
                                : Convert.ToString(reader["GRD_CD"]),

                            MajorUom = reader["MAJ_UOM"] == DBNull.Value
                                ? null
                                : Convert.ToString(reader["MAJ_UOM"]),

                            QuantityOnHand = reader["QTY_ON_HAND"] == DBNull.Value
                                ? 0m
                                : Convert.ToDecimal(reader["QTY_ON_HAND"]),

                            UnitPrice = reader["UNIT_PRICE"] == DBNull.Value
                                ? 0m
                                : Convert.ToDecimal(reader["UNIT_PRICE"]),

                            Value = reader["VALUE"] == DBNull.Value
                                ? 0m
                                : Convert.ToDecimal(reader["VALUE"]),

                            Status = reader["STATUS"] == DBNull.Value
                                ? null
                                : Convert.ToString(reader["STATUS"]),

                            CostCenterName = reader["CCT_NAME"] == DBNull.Value
                                ? null
                                : Convert.ToString(reader["CCT_NAME"])
                        };

                        result.Add(item);
                    }
                }
            }

            return result;
        }
    }
}