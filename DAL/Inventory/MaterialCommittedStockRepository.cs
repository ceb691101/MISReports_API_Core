using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class MaterialCommittedStockRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Get Provinces
        public async Task<List<MaterialCommittedStockProvinceModel>> GetProvinces()
        {
            var result = new List<MaterialCommittedStockProvinceModel>();

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
SELECT DISTINCT g.comp_id AS COMP_ID,
                c.comp_nm AS COMP_NM
FROM gldeptm g
JOIN glcompm c ON c.comp_id = g.comp_id
ORDER BY g.comp_id";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new MaterialCommittedStockProvinceModel
                        {
                            CompId = reader["COMP_ID"]?.ToString().Trim(),
                            CompNm = reader["COMP_NM"]?.ToString().Trim()
                        });
                    }
                }
            }

            return result;
        }

        // Get Material Committed Stock — flat detail rows matching user's exact query
        public async Task<List<MaterialCommittedStockModel>> GetMaterialCommittedStock(string compId, string matCode = null)
        {
            var resultList = new List<MaterialCommittedStockModel>();

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                string safeCompId = compId.Replace("'", "").Trim();
                string safeMatCode = (matCode ?? "").Replace("'", "").Trim();

                string sql = $@"
SELECT A.dept_id,
       A.wrh_cd AS WRH_CD,
       A.mat_cd AS MAT_CD,
       D.mat_nm AS MAT_NM,
       A.grade_cd AS GRADE_CD,
       D.maj_uom AS MAJOR,
       A.qty_on_hand AS QTY_ON_HAND,
       A.unit_price AS UNIT_PRICE,
       (A.unit_price * A.qty_on_hand) AS VALUE
FROM inwrhmtm A
INNER JOIN inmatm D ON A.mat_cd = D.mat_cd
WHERE A.dept_id IN (
      SELECT dept_id
      FROM gldeptm
      WHERE comp_id IN (
          SELECT comp_id
          FROM glcompm
          WHERE comp_id = '{safeCompId}' OR parent_id = '{safeCompId}'
      )
)
  AND A.mat_cd LIKE '%' || '{safeMatCode}' || '%'
  AND A.status = 2
GROUP BY A.dept_id, A.wrh_cd, A.mat_cd, D.mat_nm, A.grade_cd, D.maj_uom, A.qty_on_hand, A.unit_price, A.mat_cost
ORDER BY A.dept_id, A.wrh_cd, A.mat_cd";

                using (var cmd = new OracleCommand(sql, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        resultList.Add(new MaterialCommittedStockModel
                        {
                            DeptId = reader["DEPT_ID"]?.ToString().Trim(),
                            WrhCd = reader["WRH_CD"]?.ToString().Trim(),
                            MatCd = reader["MAT_CD"]?.ToString().Trim(),
                            MatNm = reader["MAT_NM"]?.ToString().Trim(),
                            GradeCd = reader["GRADE_CD"]?.ToString().Trim(),
                            Major = reader["MAJOR"]?.ToString().Trim(),
                            QtyOnHand = reader["QTY_ON_HAND"] != DBNull.Value
                                ? Convert.ToDecimal(reader["QTY_ON_HAND"]) : 0,
                            UnitPrice = reader["UNIT_PRICE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["UNIT_PRICE"]) : 0,
                            Value = reader["VALUE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["VALUE"]) : 0
                        });
                    }
                }
            }

            return resultList;
        }
    }
}