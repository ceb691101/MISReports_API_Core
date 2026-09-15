using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public interface IAvgConsumptionSelectedDataRepository
    {
        List<AvgConsumptionSelectedDataModel> GetSelectedAverageConsumption(
            string costCenter,
            string warehouseCode,
            DateTime fromDate,
            DateTime toDate,
            string matCode = null);
    }

    public class AvgConsumptionSelectedDataRepository : IAvgConsumptionSelectedDataRepository
    {
        private readonly string connectionString = ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public List<AvgConsumptionSelectedDataModel> GetSelectedAverageConsumption(
            string costCenter,
            string warehouseCode,
            DateTime fromDate,
            DateTime toDate,
            string matCode = null)
        {
            var resultList = new List<AvgConsumptionSelectedDataModel>();

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
        (EXTRACT(YEAR FROM TO_DATE(:toDate,'yyyy/mm/dd')) - EXTRACT(YEAR FROM TO_DATE(:fromDate,'yyyy/mm/dd'))) * 12
        + (EXTRACT(MONTH FROM TO_DATE(:toDate,'yyyy/mm/dd')) - EXTRACT(MONTH FROM TO_DATE(:fromDate,'yyyy/mm/dd')))
        + CASE
            WHEN EXTRACT(DAY FROM TO_DATE(:toDate,'yyyy/mm/dd')) >= EXTRACT(DAY FROM TO_DATE(:fromDate,'yyyy/mm/dd'))
            THEN 1
            ELSE 0
          END,
        0
      ),
      2
    ) AS Average,
    (SELECT dept_nm FROM gldeptm WHERE dept_id = :costCenter) AS CCT_NAME
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
               AND T3.trx_dt >= TO_DATE(:fromDate,'yyyy/mm/dd')
               AND T3.trx_dt <= TO_DATE(:toDate,'yyyy/mm/dd')
WHERE
    T2.mat_cd = T1.mat_cd
    AND T1.dept_id = :costCenter
    AND T1.status = 2
    AND TRIM(T1.wrh_cd) = :warehouseCode
    AND (:matCode IS NULL OR TRIM(T1.mat_cd) = TRIM(:matCode))
GROUP BY
    T1.wrh_cd, T1.mat_cd, T2.mat_nm, T1.grade_cd, T1.qty_on_hand, T1.unit_price
ORDER BY
    T1.wrh_cd ASC, T1.mat_cd ASC, T2.mat_nm ASC, T1.grade_cd ASC";

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

                        cmd.Parameters.Add("costCenter", OracleDbType.Varchar2).Value = costCenter?.Trim() ?? "";
                        cmd.Parameters.Add("warehouseCode", OracleDbType.Varchar2).Value = warehouseCode?.Trim() ?? "";
                        cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                        cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");
                        cmd.Parameters.Add("matCode", OracleDbType.Varchar2).Value =
                            string.IsNullOrWhiteSpace(matCode) ? (object)DBNull.Value : matCode.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                resultList.Add(new AvgConsumptionSelectedDataModel
                                {
                                    WarehouseCode = reader["wrh_cd"]?.ToString(),
                                    MaterialCode = reader["mat_cd"]?.ToString(),
                                    MaterialName = reader["mat_nm"]?.ToString(),
                                    GradeCode = reader["grade_cd"]?.ToString(),
                                    UnitPrice = reader.GetSafeDecimal("unit_price"),
                                    QuantityOnHand = reader.GetSafeDecimal("qty_on_hand"),
                                    TransactionQuantity = reader.GetSafeDecimal("Trx"),
                                    AverageConsumption = reader.GetSafeDecimal("Average"),
                                    CostCenterName = reader["CCT_NAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetSelectedAverageConsumption: " + ex.Message);
                throw;
            }

            return resultList;
        }
    }

    // Optional helper extension method
    internal static class OracleDataReaderExtensions
    {
        public static decimal GetSafeDecimal(this OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetDecimal(ordinal) : 0m;
        }
    }
}