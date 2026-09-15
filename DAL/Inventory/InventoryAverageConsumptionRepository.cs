using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class InventoryAverageConsumptionRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<InventoryAverageConsumption> GetAverageConsumption(string costCenter, string warehouseCode, DateTime parsedFromDate, DateTime parsedToDate)
        {
            var consumptionList = new List<InventoryAverageConsumption>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    // === LOG INPUTS FOR DEBUGGING ===
                    Debug.WriteLine("=== GetAverageConsumption Debug ===");
                    Debug.WriteLine($"Cost Center: '{costCenter}'");
                    Debug.WriteLine($"Warehouse Code: '{warehouseCode}'");
                    Debug.WriteLine($"From Date: {parsedFromDate:yyyy-MM-dd} → '{parsedFromDate:yyyy/MM/dd}'");
                    Debug.WriteLine($"To Date: {parsedToDate:yyyy-MM-dd} → '{parsedToDate:yyyy/MM/dd}'");

                    string sql = @"
SELECT
    T1.wrh_cd,
    T1.mat_cd,
    T2.mat_nm,
    T1.grade_cd,
    T1.unit_price,
    T1.qty_on_hand,
    NVL(SUM(CASE WHEN T3.add_deduct='F' THEN T3.trx_qty
                 WHEN T3.add_deduct='T' THEN -T3.trx_qty
                 ELSE 0.00 END), 0.00) AS Trx,
    ROUND(
      NVL(SUM(CASE WHEN T3.add_deduct='F' THEN T3.trx_qty
                   WHEN T3.add_deduct='T' THEN -T3.trx_qty
                   ELSE 0.00 END), 0.00)
      /
      NULLIF(
        -- CORRECT: Count full months (inclusive)
        (EXTRACT(YEAR FROM TO_DATE(:parsedToDate,'yyyy/mm/dd')) - EXTRACT(YEAR FROM TO_DATE(:parsedFromDate,'yyyy/mm/dd'))) * 12
        + (EXTRACT(MONTH FROM TO_DATE(:parsedToDate,'yyyy/mm/dd')) - EXTRACT(MONTH FROM TO_DATE(:parsedFromDate,'yyyy/mm/dd')))
        + CASE
            WHEN EXTRACT(DAY FROM TO_DATE(:parsedToDate,'yyyy/mm/dd')) >= EXTRACT(DAY FROM TO_DATE(:parsedFromDate,'yyyy/mm/dd'))
            THEN 1
            ELSE 0
          END,
        0
      ),
      2
    ) AS Average,
    (SELECT dept_nm FROM gldeptm WHERE dept_id=:costCenter) AS CCT_NAME
FROM
    inmatm T2,
    inwrhmtm T1
LEFT OUTER JOIN
    inpostmt T3 ON T1.dept_id = T3.dept_id
               AND T1.mat_cd = T3.mat_cd
               AND T1.grade_cd = T3.grade_cd
               AND T1.wrh_cd = T3.wrh_cd
               AND (T3.trx_type LIKE 'ISS%' OR
                   (T3.trx_type IN ('RECEIPT') AND T3.doc_pf IN ('RTV','RTV-CL')))
               AND T3.trx_dt >= TO_DATE(:parsedFromDate,'yyyy/mm/dd')
               AND T3.trx_dt <= TO_DATE(:parsedToDate,'yyyy/mm/dd')
WHERE
    T2.mat_cd = T1.mat_cd
    AND T1.dept_id = :costCenter
    AND T1.status = 2
    AND TRIM(T1.wrh_cd) = :warehouseCode
GROUP BY
    T1.wrh_cd, T1.mat_cd, T2.mat_nm, T1.grade_cd, T1.qty_on_hand, T1.unit_price
ORDER BY
    T1.wrh_cd ASC, T1.mat_cd ASC, T2.mat_nm ASC, T1.grade_cd ASC, T1.qty_on_hand ASC";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

                        // === CRITICAL FIX: Use Varchar2 + yyyy/MM/dd format ===
                        cmd.Parameters.Add("costCenter", OracleDbType.Varchar2).Value = costCenter;
                        cmd.Parameters.Add("warehouseCode", OracleDbType.Varchar2).Value = warehouseCode?.Trim();

                        cmd.Parameters.Add("parsedFromDate", OracleDbType.Varchar2).Value = parsedFromDate.ToString("yyyy/MM/dd");
                        cmd.Parameters.Add("parsedToDate", OracleDbType.Varchar2).Value = parsedToDate.ToString("yyyy/MM/dd");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                consumptionList.Add(new InventoryAverageConsumption
                                {
                                    WarehouseCode = reader["wrh_cd"]?.ToString(),
                                    MaterialCode = reader["mat_cd"]?.ToString(),
                                    MaterialName = reader["mat_nm"]?.ToString(),
                                    GradeCode = reader["grade_cd"]?.ToString(),
                                    UnitPrice = reader["unit_price"] != DBNull.Value ? Convert.ToDecimal(reader["unit_price"]) : 0,
                                    QuantityOnHand = reader["qty_on_hand"] != DBNull.Value ? Convert.ToDecimal(reader["qty_on_hand"]) : 0,
                                    TransactionQuantity = reader["Trx"] != DBNull.Value ? Convert.ToDecimal(reader["Trx"]) : 0,
                                    AverageConsumption = reader["Average"] != DBNull.Value ? Convert.ToDecimal(reader["Average"]) : 0,
                                    CostCenterName = reader["CCT_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAverageConsumption: {ex.Message}\n{ex.StackTrace}");
                throw;
            }

            return consumptionList;
        }

        public List<Warehouse> GetWarehousesByEpfNoAndCostCenter(string epfNo, string costCenterId)
        {
            var warehouseList = new List<Warehouse>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                             SELECT DISTINCT(wrh_cd)
                                FROM inwrhm
                                WHERE status = 2
                                    AND TRIM(dept_id) = TRIM(:costCenterId)
                                    AND TRIM(dept_id) IN (
                                            SELECT TRIM(costcentre)
                                            FROM rep_roles_cct_new cct, rep_role_new r
                                            WHERE TRIM(r.roleid) = TRIM(cct.roleid)
                                                AND cct.lvl_no = 0
                                                AND TRIM(r.epf_no) = TRIM(:epfNo))";


              

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add("costCenterId", OracleDbType.Varchar2).Value = costCenterId;
                        cmd.Parameters.Add("epfNo", OracleDbType.Varchar2).Value = epfNo;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                warehouseList.Add(new Warehouse
                                {
                                    WarehouseCode = reader["wrh_cd"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetWarehousesByEpfNoAndCostCenter: {ex.Message}");
                throw;
            }

            return warehouseList;
        }
    }
}