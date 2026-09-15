using MISReports_Api.Models.PhysicalVerification;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.PhysicalVerification
{
    public class PHVSlowNonMovingWHwiseRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<PHVSlowNonMovingWHwiseModel>> GetSlowNonMovingWHwiseAsync(
            string deptId,
            int repYear,
            int repMonth,
            string warehouseCode)
        {
            var result = new List<PHVSlowNonMovingWHwiseModel>();

            var fromDate = new DateTime(repYear, repMonth, 1);
            var toDate = fromDate.AddMonths(1);

            string sql = @"
                SELECT
                    CASE
                        WHEN SUBSTR(TRIM(T1.REASON),1,1) = 'N' THEN '1. Non-moving'
                        WHEN SUBSTR(TRIM(T1.REASON),1,1) = 'S' THEN '2. Slow-moving'
                        ELSE NULL
                    END AS STATUS,
                    T3.PHV_DT,
                    T1.MAT_CD,
                    T2.MAT_NM,
                    T4.UOM_CD,
                    T4.GRADE_CD,
                    T5.UNIT_COST AS UNIT_PRICE,
                    T1.CNTED_QTY AS QTY_ON_HAND,
                    T1.DOC_NO,
                    (T1.CNTED_QTY * T5.UNIT_COST) AS STOCKBOOK,
                    T1.REASON
                FROM INPHVDMT T1
                JOIN INPOSTMT T5
                    ON TRIM(T1.DOC_NO)   = TRIM(T5.DOC_NO)
                   AND TRIM(T1.DOC_PF)   = TRIM(T5.DOC_PF)
                   AND TRIM(T1.DEPT_ID)  = TRIM(T5.DEPT_ID)
                   AND TRIM(T1.MAT_CD)   = TRIM(T5.MAT_CD)
                   AND TRIM(T1.GRADE_CD) = TRIM(T5.GRADE_CD)
                JOIN INMATM T2
                    ON TRIM(T5.MAT_CD) = TRIM(T2.MAT_CD)
                JOIN INPHVHMT T3
                    ON TRIM(T3.DOC_NO)   = TRIM(T1.DOC_NO)
                   AND TRIM(T3.DOC_PF)   = TRIM(T1.DOC_PF)
                   AND TRIM(T3.DEPT_ID)  = TRIM(T1.DEPT_ID)
                   AND TRIM(T3.BATCH_ID) = TRIM(T1.BATCH_ID)
                JOIN INWRHMTM T4
                    ON TRIM(T5.DEPT_ID)  = TRIM(T4.DEPT_ID)
                   AND TRIM(T5.MAT_CD)   = TRIM(T4.MAT_CD)
                   AND TRIM(T5.GRADE_CD) = TRIM(T4.GRADE_CD)
                   AND TRIM(T3.WRH_CD)   = TRIM(T4.WRH_CD)
                WHERE
                    T4.STATUS IN (2,7)
                    AND TRIM(T1.DEPT_ID) = TRIM(:dept_id)
                    AND T3.PHV_DT >= :from_date
                    AND T3.PHV_DT <  :to_date
                    AND TRIM(T3.WRH_CD) = TRIM(:wrh_cd)
                    AND SUBSTR(TRIM(T1.REASON),1,1) IN ('S','N')
                ORDER BY
                    STATUS,
                    T1.DOC_NO,
                    T1.MAT_CD,
                    T4.GRADE_CD";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;

                cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = deptId?.Trim();
                cmd.Parameters.Add("from_date", OracleDbType.Date).Value = fromDate;
                cmd.Parameters.Add("to_date", OracleDbType.Date).Value = toDate;
                cmd.Parameters.Add("wrh_cd", OracleDbType.Varchar2).Value = warehouseCode?.Trim();

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new PHVSlowNonMovingWHwiseModel
                        {
                            Status = reader["STATUS"]?.ToString(),
                            PhvDate = reader["PHV_DT"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("PHV_DT")),
                            MaterialCode = reader["MAT_CD"]?.ToString(),
                            MaterialName = reader["MAT_NM"]?.ToString(),
                            UomCode = reader["UOM_CD"]?.ToString(),
                            GradeCode = reader["GRADE_CD"]?.ToString(),
                            UnitPrice = reader["UNIT_PRICE"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["UNIT_PRICE"]),
                            QtyOnHand = reader["QTY_ON_HAND"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["QTY_ON_HAND"]),
                            DocumentNo = reader["DOC_NO"]?.ToString(),
                            StockBook = reader["STOCKBOOK"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["STOCKBOOK"]),
                            Reason = reader["REASON"]?.ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}