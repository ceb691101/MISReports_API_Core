using MISReports_Api.Compat;
using MISReports_Api.Models.Inventory;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;

namespace MISReports_Api.DAL.Inventory
{
    public class MaterialPriceByYearDAL
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<MaterialPriceByYearModel> GetMaterialPriceByYear(
            string costCtr,
            string repYear)
        {
            var materialPrices = new List<MaterialPriceByYearModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                string sql = @"
                    SELECT DISTINCT
                           T1.WRH_CD,
                           T1.MAT_CD,
                           T2.MAT_NM,
                           T3.FIN_MTH,
                           T1.NEW_PRICE AS UNIT_PRICE,
                           (
                               SELECT DEPT_NM
                               FROM GLDEPTM
                               WHERE DEPT_ID = :costctr
                           ) AS CCT_NAME
                    FROM INADJDMT T1
                    JOIN INADJBTM T3
                        ON T1.BATCH_ID = T3.BATCH_ID
                       AND T1.DEPT_ID = T3.DEPT_ID
                    JOIN INMATM T2
                        ON T2.MAT_CD = T1.MAT_CD
                    WHERE T3.STATUS >= 5
                      AND T3.FIN_YR = :repyear
                      AND T1.DEPT_ID = :costctr
                    ORDER BY T1.MAT_CD,
                             T3.FIN_MTH";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(
                        new OracleParameter("costctr", costCtr.Trim()));

                    cmd.Parameters.Add(
                        new OracleParameter("repyear", repYear.Trim()));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            materialPrices.Add(new MaterialPriceByYearModel
                            {
                                WrhCd = reader["WRH_CD"]?.ToString().Trim(),

                                MatCd = reader["MAT_CD"]?.ToString().Trim(),

                                MatNm = reader["MAT_NM"]?.ToString().Trim(),

                                FinMth = reader["FIN_MTH"]?.ToString().Trim(),

                                UnitPrice = reader["UNIT_PRICE"] != DBNull.Value
                                    ? (decimal?)Convert.ToDecimal(reader["UNIT_PRICE"])
                                    : null,

                                CctName = reader["CCT_NAME"]?.ToString().Trim()
                            });
                        }
                    }
                }
            }

            return materialPrices;
        }
    }
}