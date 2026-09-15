using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.PhysicalVerification
{
    public class PhysicalVerificationRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<PHVEntryFormModel>> GetPhysicalVerificationDataAsync(
            string deptId,
            string docNo)
        {
            var result = new List<PHVEntryFormModel>();

            string sql = @"
                SELECT DISTINCT
                    T1.MAT_CD,
                    T2.MAT_NM,
                    T4.UOM_CD,
                    T4.GRADE_CD,
                    T1.QTY_ON_HAND,
                    T1.CNTED_QTY,
                    T4.UNIT_PRICE
                FROM
                    INPHVDTT T1,
                    INMATM   T2,
                    INPHVHTT T3,
                    INWRHMTM T4
                WHERE
                    T3.DOC_NO   = T1.DOC_NO
                    AND T3.DOC_PF   = T1.DOC_PF
                    AND T3.DEPT_ID  = T1.DEPT_ID
                    AND T4.DEPT_ID  = T1.DEPT_ID
                    AND T1.MAT_CD   = T2.MAT_CD
                    AND T1.MAT_CD   = T4.MAT_CD
                    AND T1.GRADE_CD = T4.GRADE_CD
                    AND T4.STATUS IN (7)
                    AND T3.STATUS IN (1)
                    AND TRIM(T1.DEPT_ID) = :dept_id
                    AND TRIM(T3.DOC_NO)  = :doc_no

                ORDER BY
                    T1.MAT_CD,
                    T4.GRADE_CD";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = deptId;
                cmd.Parameters.Add("doc_no", OracleDbType.Varchar2).Value = docNo;

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new PHVEntryFormModel
                        {
                            MatCd = reader["MAT_CD"]?.ToString(),
                            MatNm = reader["MAT_NM"]?.ToString(),
                            UomCd = reader["UOM_CD"]?.ToString(),
                            GradeCd = reader["GRADE_CD"]?.ToString(),

                            QtyOnHand = reader["QTY_ON_HAND"] != DBNull.Value
                                ? Convert.ToDecimal(reader["QTY_ON_HAND"])
                                : 0,

                            CntedQty = reader["CNTED_QTY"] != DBNull.Value
                                ? Convert.ToDecimal(reader["CNTED_QTY"])
                                : 0,

                            UnitPrice = reader["UNIT_PRICE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["UNIT_PRICE"])
                                : 0
                        });
                    }
                }
            }

            return result;
        }
    }
}
