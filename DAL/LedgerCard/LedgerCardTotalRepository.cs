using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class LedgerCardTotalRepository
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<LedgerCardTotalModel> GetLedgerCardTotalData(
            string glCode, int repYear, int repMonth)
        {
            var result = new List<LedgerCardTotalModel>();

            string sql = @"
SELECT
    T1.gl_cd                     AS GlCd,
    TRIM(T1.sub_ac) || '-' || T2.ac_nm AS SubAc,
    T1.op_bal                    AS OpBal,
    T1.dr_amt                    AS DrAmt,
    T1.cr_amt                    AS CrAmt,
    T1.cl_bal                    AS ClBal,
    (SELECT gl_nm
       FROM glledgrm
      WHERE TRIM(gl_cd) = :GLCODE)   AS AcName,
    (SELECT T4.op_bal
       FROM gllegbal T4
      WHERE TRIM(T4.gl_cd) = :GLCODE
        AND T4.yr_ind = :REPYEAR
        AND T4.mth_ind = :REPMONTH) AS GLOpeningBalance,
    (SELECT T4.cl_bal
       FROM gllegbal T4
      WHERE TRIM(T4.gl_cd) = :GLCODE
        AND T4.yr_ind = :REPYEAR
        AND T4.mth_ind = :REPMONTH) AS GLClosingBalance,
    (SELECT dept_nm
       FROM gldeptm
      WHERE dept_id = SUBSTR(:GLCODE, 1, 6)) AS CctName
FROM glsubbal T1
JOIN glsubacm T2
  ON T1.gl_cd   = T2.gl_cd
 AND T1.sub_ac  = T2.sub_ac
WHERE TRIM(T1.gl_cd)   = :GLCODE
  AND T2.status  = 2
  AND T1.yr_ind  = :REPYEAR
  AND T1.mth_ind = :REPMONTH
ORDER BY T1.gl_cd, T1.sub_ac";

            using (var connection = new OracleConnection(connectionString))
            using (var command = new OracleCommand(sql, connection))
            {
                command.BindByName = true;
                command.Parameters.Add("GLCODE", OracleDbType.Varchar2).Value = glCode;
                command.Parameters.Add("REPYEAR", OracleDbType.Int32).Value = repYear;
                command.Parameters.Add("REPMONTH", OracleDbType.Int32).Value = repMonth;

                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new LedgerCardTotalModel
                            {
                                GlCd = SafeGetString(reader, "GlCd"),
                                SubAc = SafeGetString(reader, "SubAc"),
                                OpBal = SafeGetDecimal(reader, "OpBal"),
                                DrAmt = SafeGetDecimal(reader, "DrAmt"),
                                CrAmt = SafeGetDecimal(reader, "CrAmt"),
                                ClBal = SafeGetDecimal(reader, "ClBal"),
                                AcName = SafeGetString(reader, "AcName"),
                                GLOpeningBalance = SafeGetDecimal(reader, "GLOpeningBalance"),
                                GLClosingBalance = SafeGetDecimal(reader, "GLClosingBalance"),
                                CctName = SafeGetString(reader, "CctName")
                            };
                            result.Add(item);
                        }
                    }
                }
                catch (OracleException oex)
                {
                    throw new Exception(
                        $"Oracle error while fetching Ledger Card Total for GL {glCode}. Code: {oex.Number}",
                        oex);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"Error fetching Ledger Card Total for GL {glCode}.", ex);
                }
            }

            return result;
        }

        #region Safe readers (same as in LedgerCardRepository)

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

        #endregion Safe readers (same as in LedgerCardRepository)
    }
}