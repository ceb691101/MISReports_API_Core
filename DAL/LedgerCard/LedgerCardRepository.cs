using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class LedgerCardRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<LedgerCardModel> GetLedgerCardData(string glCode, int repYear, int startMonth, int endMonth)
        {
            var result = new List<LedgerCardModel>();

            string sql = @"
   SELECT
    T1.gl_cd AS GlCd,
    T1.sub_ac AS SubAc,
    T2.remarks AS Remarks,
    T2.acct_dt AS AcctDt,
    T1.doc_pf AS DocPf,
    T1.doc_no AS DocNo,
    T2.ref_1 AS Ref1,
    T2.chq_no AS ChqNo,
    T1.cr_amt AS CrAmt,
    T1.dr_amt AS DrAmt,
    T5.op_bal AS OpeningBalance,
    T6.cl_bal AS ClosingBalance,
    T1.seq_no AS SeqNo,
    T2.log_mth AS LogMth,
    (SELECT T4.op_bal
       FROM gllegbal T4
      WHERE TRIM(T4.gl_cd) = :GLCODE
        AND T4.yr_ind = :REPYEAR
        AND T4.mth_ind = :STARTMONTH) AS OpBal,
    (SELECT T4.cl_bal
       FROM gllegbal T4
      WHERE TRIM(T4.gl_cd) = :GLCODE
        AND T4.yr_ind = :REPYEAR
        AND T4.mth_ind = :ENDMONTH) AS ClBal,
    (SELECT gl_nm
       FROM glledgrm
      WHERE TRIM(gl_cd) = :GLCODE) AS AcName,
    (SELECT ac_nm
       FROM glsubacm
      WHERE TRIM(gl_cd) = :GLCODE
        AND sub_ac = T1.sub_ac
        AND status = 2) AS AcName1,
    (SELECT dept_nm
       FROM gldeptm
      WHERE dept_id = SUBSTR(:GLCODE, 1, 6)) AS CctName
FROM
    glvocdmt T1
    INNER JOIN glvochmt T2
        ON T1.doc_no = T2.doc_no
       AND T1.batch_id = T2.batch_id
       AND T1.doc_pf = T2.doc_pf
       AND T1.dept_id = T2.dept_id
    INNER JOIN glsubbal T5
        ON T1.gl_cd = T5.gl_cd
       AND T1.sub_ac = T5.sub_ac
       AND T5.yr_ind = :REPYEAR
       AND T5.mth_ind = :STARTMONTH
    INNER JOIN glsubbal T6
        ON T1.gl_cd = T6.gl_cd
       AND T1.sub_ac = T6.sub_ac
       AND T6.yr_ind = :REPYEAR
       AND T6.mth_ind = :ENDMONTH
WHERE
    TRIM(T1.gl_cd) = :GLCODE
    AND T2.status = 6
    AND T2.log_yr = :REPYEAR
    AND T2.log_mth BETWEEN :STARTMONTH AND :ENDMONTH
GROUP BY
    T1.gl_cd, T1.doc_pf, T1.doc_no, T2.log_mth, T1.sub_ac,
    T2.acct_dt, T2.chq_no, T2.ref_1, T1.cr_amt, T1.dr_amt,
    T5.op_bal, T6.cl_bal, T2.remarks, T1.seq_no
ORDER BY
    T1.gl_cd, T1.sub_ac, T2.log_mth, T2.acct_dt,
    T1.doc_pf, T1.doc_no, T2.chq_no, T2.ref_1,
    T1.cr_amt, T1.dr_amt";

            using (var connection = new OracleConnection(connectionString))
            {
                using (var command = new OracleCommand(sql, connection))
                {
                    command.BindByName = true;

                    command.Parameters.Add("GLCODE", OracleDbType.Varchar2).Value = glCode;
                    command.Parameters.Add("REPYEAR", OracleDbType.Int32).Value = repYear;
                    command.Parameters.Add("STARTMONTH", OracleDbType.Int32).Value = startMonth;
                    command.Parameters.Add("ENDMONTH", OracleDbType.Int32).Value = endMonth;

                    try
                    {
                        connection.Open();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var item = new LedgerCardModel
                                {
                                    GlCd = SafeGetString(reader, "GlCd"),
                                    SubAc = SafeGetString(reader, "SubAc"),
                                    Remarks = SafeGetString(reader, "Remarks"),
                                    AcctDt = SafeGetDateTime(reader, "AcctDt"),
                                    DocPf = SafeGetString(reader, "DocPf"),
                                    DocNo = SafeGetString(reader, "DocNo"),
                                    Ref1 = SafeGetString(reader, "Ref1"),
                                    ChqNo = SafeGetString(reader, "ChqNo"),
                                    CrAmt = SafeGetDecimal(reader, "CrAmt"),
                                    DrAmt = SafeGetDecimal(reader, "DrAmt"),
                                    SeqNo = SafeGetInt32(reader, "SeqNo"),
                                    LogMth = SafeGetInt32(reader, "LogMth"),
                                    OpeningBalance = SafeGetDecimal(reader, "OpeningBalance"),
                                    ClosingBalance = SafeGetDecimal(reader, "ClosingBalance"),
                                    GLOpeningBalance = SafeGetDecimal(reader, "OpBal"),
                                    GLClosingBalance = SafeGetDecimal(reader, "ClBal"),
                                    AcName = SafeGetString(reader, "AcName"),
                                    AcName1 = SafeGetString(reader, "AcName1"),
                                    CctName = SafeGetString(reader, "CctName")
                                };
                                result.Add(item);
                            }
                        }
                    }
                    catch (OracleException oex)
                    {
                        throw new Exception($"Oracle database error occurred while fetching Ledger Card data for GL Code {glCode}. Error: {oex.Message} (Code: {oex.Number})", oex);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Database error occurred while fetching Ledger Card data for GL Code {glCode}.", ex);
                    }
                }
            }

            return result;
        }

        // Helper methods for safe data reading
        private string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private DateTime? SafeGetDateTime(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }

        private decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal);
        }

        private int? SafeGetInt32(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }
    }
}