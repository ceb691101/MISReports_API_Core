using MISReports_Api.Models.PhysicalVerification;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.PhysicalVerification
{
    public class AnnualVerificationSheetRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<AnnualVerificationSheetModel>> GetAnnualVerificationSheetAsync(
            string deptId,
            int repYear,
            int repMonth)
        {
            var result = new List<AnnualVerificationSheetModel>();

            string sql = @"
                SELECT
                    T1.MAT_CD,
                    T2.MAT_NM,
                    T4.UOM_CD,
                    T4.GRADE_CD,
                    T5.UNIT_COST AS UNIT_PRICE,
                    T1.QTY_ON_HAND,
                    T1.CNTED_QTY,
                    T1.DOC_NO,
                    T3.PHV_DT,

                    CASE WHEN T1.ADJ_QTY > 0 THEN T1.ADJ_QTY ELSE NULL END AS SURPLUS,
                    CASE WHEN T1.ADJ_QTY < 0 THEN T1.ADJ_QTY ELSE NULL END AS SHORTAGE,

                    (T1.QTY_ON_HAND * T5.UNIT_COST) AS STOCKBOOK,
                    (T1.CNTED_QTY * T5.UNIT_COST) AS PHYSICALBOOK,

                    CASE WHEN T1.ADJ_QTY > 0 THEN T1.ADJ_QTY * T5.UNIT_COST ELSE NULL END AS SURPLUS_AMT,
                    CASE WHEN T1.ADJ_QTY < 0 THEN T1.ADJ_QTY * T5.UNIT_COST ELSE NULL END AS SHORTAGE_AMT,

                    T1.REASON
                FROM
                    INPHVDMT T1
                    JOIN INPOSTMT T5 ON T1.DOC_NO = T5.DOC_NO
                                    AND T1.DOC_PF = T5.DOC_PF
                                    AND T1.DEPT_ID = T5.DEPT_ID
                                    AND T1.MAT_CD = T5.MAT_CD
                                    AND T1.GRADE_CD = T5.GRADE_CD
                    JOIN INMATM T2 ON T5.MAT_CD = T2.MAT_CD
                    JOIN INPHVHMT T3 ON T3.DOC_NO = T1.DOC_NO
                                    AND T3.DOC_PF = T1.DOC_PF
                                    AND T3.DEPT_ID = T1.DEPT_ID
                                    AND T3.BATCH_ID = T1.BATCH_ID
                    JOIN INWRHMTM T4 ON T5.DEPT_ID = T4.DEPT_ID
                                    AND T5.MAT_CD = T4.MAT_CD
                                    AND T5.GRADE_CD = T4.GRADE_CD
                                    AND T5.WRH_CD = T4.WRH_CD
                WHERE
                    T4.STATUS IN (7)
                    AND TRIM(T1.DEPT_ID) = :dept_id
                    AND TO_CHAR(T3.PHV_DT,'YYYY') = :rep_year
                    AND TO_CHAR(T3.PHV_DT,'MM') = :rep_month
                ORDER BY
                    T1.DOC_NO,
                    T1.MAT_CD,
                    T4.GRADE_CD";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = deptId.Trim();
                cmd.Parameters.Add("rep_year", OracleDbType.Varchar2).Value = repYear.ToString();
                cmd.Parameters.Add("rep_month", OracleDbType.Varchar2).Value = repMonth.ToString("D2");

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new AnnualVerificationSheetModel
                        {
                            MaterialCode = reader["MAT_CD"]?.ToString(),
                            MaterialName = reader["MAT_NM"]?.ToString(),
                            UomCode = reader["UOM_CD"]?.ToString(),
                            GradeCode = reader["GRADE_CD"]?.ToString(),

                            UnitPrice = reader["UNIT_PRICE"] != DBNull.Value ? Convert.ToDecimal(reader["UNIT_PRICE"]) : 0,
                            QtyOnHand = reader["QTY_ON_HAND"] != DBNull.Value ? Convert.ToDecimal(reader["QTY_ON_HAND"]) : 0,
                            CountedQty = reader["CNTED_QTY"] != DBNull.Value ? Convert.ToDecimal(reader["CNTED_QTY"]) : 0,

                            DocumentNo = reader["DOC_NO"]?.ToString(),
                            PhvDate = reader["PHV_DT"]?.ToString(),

                            SurplusQty = reader["SURPLUS"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["SURPLUS"]) : null,
                            ShortageQty = reader["SHORTAGE"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["SHORTAGE"]) : null,

                            StockBook = reader["STOCKBOOK"] != DBNull.Value ? Convert.ToDecimal(reader["STOCKBOOK"]) : 0,
                            PhysicalBook = reader["PHYSICALBOOK"] != DBNull.Value ? Convert.ToDecimal(reader["PHYSICALBOOK"]) : 0,

                            SurplusAmount = reader["SURPLUS_AMT"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["SURPLUS_AMT"]) : null,
                            ShortageAmount = reader["SHORTAGE_AMT"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["SHORTAGE_AMT"]) : null,

                            Reason = reader["REASON"]?.ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}
