using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.PhysicalVerification
{
    public class PHVValidationRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<PHVValidationModel>> GetPHVValidationDataAsync(
            string deptId,
            string repYear,
            string repMonth)
        {
            var result = new List<PHVValidationModel>();

            string sql = @"
                SELECT DISTINCT
                    T1.MAT_CD,
                    T2.MAT_NM,
                    T4.UOM_CD,
                    T4.GRADE_CD,
                    T1.QTY_ON_HAND,
                    T1.CNTED_QTY,
                    T4.UNIT_PRICE,
                    T1.REASON
                FROM
                    INPHVDTT T1,
                    INMATM   T2,
                    INPHVHTT T3,
                    INWRHMTM T4
                WHERE
                    T3.DOC_NO   = T1.DOC_NO
                    AND T3.DOC_PF   = T1.DOC_PF
                    AND T1.MAT_CD   = T2.MAT_CD
                    AND T1.MAT_CD   = T4.MAT_CD
                    AND T1.GRADE_CD = T4.GRADE_CD

                   
                    AND TRIM(T3.DEPT_ID) = :dept_id
                    AND TRIM(T4.DEPT_ID) = :dept_id
                    AND TRIM(T1.DEPT_ID) = :dept_id

                    AND TO_CHAR(T3.PHV_DT,'YYYY') = :rep_year
                    AND TO_CHAR(T3.PHV_DT,'MM')   = :rep_month
                ORDER BY
                    T1.MAT_CD,
                    T4.GRADE_CD";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;


                cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = deptId;
                cmd.Parameters.Add("rep_year", OracleDbType.Varchar2).Value = repYear;
                cmd.Parameters.Add("rep_month", OracleDbType.Varchar2).Value = repMonth;

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new PHVValidationModel
                        {
                            MatCd = reader["MAT_CD"]?.ToString().Trim(),
                            MatNm = reader["MAT_NM"]?.ToString().Trim(),
                            UomCd = reader["UOM_CD"]?.ToString().Trim(),
                            GradeCd = reader["GRADE_CD"]?.ToString().Trim(),

                            QtyOnHand = reader["QTY_ON_HAND"] != DBNull.Value
                                ? Convert.ToDecimal(reader["QTY_ON_HAND"])
                                : 0,

                            CntedQty = reader["CNTED_QTY"] != DBNull.Value
                                ? Convert.ToDecimal(reader["CNTED_QTY"])
                                : 0,

                            UnitPrice = reader["UNIT_PRICE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["UNIT_PRICE"])
                                : 0,

                            Reason = reader["REASON"]?.ToString().Trim()
                        });
                    }
                }
            }

            return result;
        }
    }
}