using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class DivisionalLedgerCardRepository
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<DivisionalLedgerCardModel> GetDivisionalLedgerCardData(
            int year, int month, string glCode, string company)
        {
            var result = new List<DivisionalLedgerCardModel>();

            string sql = @"
SELECT
    NVL(T4.AC_NM, T1.SUB_AC) AS SUB_AC,
    T1.GL_CD,
    T1.DOC_PF,
    T1.DOC_NO,
    T1.CR_AMT,
    T1.DR_AMT,
    T2.REMARKS,
    T2.ACCT_DT,
    T2.LOG_MTH,
    T2.chq_no,
    T2.ref_1,
    T3.OP_BAL AS OPBAL,
    T3.CL_BAL AS CLBAL
FROM
    GLVOCDMT T1
    JOIN GLVOCHMT T2 ON T1.DOC_NO = T2.DOC_NO
                    AND T1.BATCH_ID = T2.BATCH_ID
                    AND T1.DOC_PF = T2.DOC_PF
                    AND T1.DEPT_ID = T2.DEPT_ID
    JOIN GLLEGBAL T3 ON T1.GL_CD = T3.GL_CD
    LEFT JOIN GLSUBACM T4 ON T4.SUB_AC = T1.SUB_AC AND T4.GL_CD = T1.GL_CD
WHERE
    T3.YR_IND = :year
    AND T3.MTH_IND = :month
    AND T2.LOG_YR = :year
    AND T2.LOG_MTH = :month
    AND SUBSTR(T1.GL_CD,8,5) = :gl_code
    AND T2.STATUS = 6
    AND T1.DEPT_ID IN (
        SELECT DEPT_ID FROM GLDEPTM
        WHERE STATUS = 2
        AND COMP_ID IN (
            SELECT COMP_ID FROM GLCOMPM
            WHERE COMP_ID = :company
               OR PARENT_ID = :company
               OR GRP_COMP = :company
        )
    )
ORDER BY
    T1.GL_CD,
    SUBSTR(T1.GL_CD,8,5),
    T1.SUB_AC,
    T2.LOG_MTH,
    T2.ACCT_DT,
    T1.DOC_PF,
    T1.DOC_NO,
    T2.chq_no,
    T2.ref_1";

            using (var connection = new OracleConnection(connectionString))
            using (var command = new OracleCommand(sql, connection))
            {
                command.BindByName = true;
                command.Parameters.Add("year", OracleDbType.Int32).Value = year;
                command.Parameters.Add("month", OracleDbType.Int32).Value = month;
                command.Parameters.Add("gl_code", OracleDbType.Varchar2).Value = glCode;
                command.Parameters.Add("company", OracleDbType.Varchar2).Value = company;

                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new DivisionalLedgerCardModel
                            {
                                SubAc = SafeGetString(reader, "SUB_AC"),
                                GlCd = SafeGetString(reader, "GL_CD"),
                                DocPf = SafeGetString(reader, "DOC_PF"),
                                DocNo = SafeGetString(reader, "DOC_NO"),
                                CrAmt = SafeGetDecimal(reader, "CR_AMT"),
                                DrAmt = SafeGetDecimal(reader, "DR_AMT"),
                                Remarks = SafeGetString(reader, "REMARKS"),
                                AcctDt = SafeGetDateTime(reader, "ACCT_DT"),
                                LogMth = SafeGetInt32(reader, "LOG_MTH"),
                                ChqNo = SafeGetString(reader, "chq_no"),
                                Ref1 = SafeGetString(reader, "ref_1"),
                                OpBal = SafeGetDecimal(reader, "OPBAL"),
                                ClBal = SafeGetDecimal(reader, "CLBAL")
                            };
                            result.Add(item);
                        }
                    }
                }
                catch (OracleException oex)
                {
                    throw new Exception(
                        $"Oracle error in DivisionalLedgerCard: Code {oex.Number}", oex);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"Error fetching Divisional Ledger Card data.", ex);
                }
            }
            return result;
        }

        #region Safe Readers

        private string SafeGetString(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? null : r.GetString(ord);
        }

        private decimal? SafeGetDecimal(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? (decimal?)null : r.GetDecimal(ord);
        }

        private DateTime? SafeGetDateTime(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? (DateTime?)null : r.GetDateTime(ord);
        }

        private int? SafeGetInt32(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? (int?)null : r.GetInt32(ord);
        }

        #endregion Safe Readers
    }
}