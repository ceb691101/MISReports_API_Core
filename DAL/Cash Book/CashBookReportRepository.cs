using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class CashBookReportRepository
    {
        private readonly string _connStr = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CashBookReportModel> GetCashBookData(string fromDate, string toDate, string payee)
        {
            string oracleFrom = FormatDateForOracle(fromDate);
            string oracleTo = FormatDateForOracle(toDate);
            var result = new List<CashBookReportModel>();

            const string sql = @"
SELECT * FROM (
    SELECT
        T1.CHQ_RUN,
        T1.CHQ_DT,
        T1.PAYEE,
        T1.PYMT_DOCNO,
        T1.CHQ_AMT,
        T1.CHQ_NO,
        'CEYLON ELECTRICITY BOARD' AS CCT_NAME
    FROM
        CBCHQDMT T1
    WHERE
        T1.CHQ_DT >= TO_DATE(:fromDate, 'YYYY/MM/DD')
        AND T1.CHQ_DT <= TO_DATE(:toDate, 'YYYY/MM/DD')
        AND (:Payee IS NULL OR UPPER(T1.PAYEE) LIKE '%' || UPPER(TRIM(:Payee)) || '%')
    GROUP BY
        T1.PAYEE, T1.CHQ_DT, T1.CHQ_NO, T1.CHQ_RUN, T1.PYMT_DOCNO, T1.CHQ_AMT
    ORDER BY
        T1.PAYEE, T1.CHQ_DT, T1.CHQ_NO
) WHERE ROWNUM <= 5000";

            using (var conn = new OracleConnection(_connStr))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = oracleFrom;
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = oracleTo;
                cmd.Parameters.Add("Payee", OracleDbType.Varchar2).Value =
                    string.IsNullOrWhiteSpace(payee) ? (object)DBNull.Value : payee.Trim();

                try
                {
                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            result.Add(new CashBookReportModel
                            {
                                ChqRun = SafeStr(rdr, "CHQ_RUN"),
                                ChqDt = SafeDate(rdr, "CHQ_DT"),
                                Payee = SafeStr(rdr, "PAYEE"),
                                PymtDocNo = SafeStr(rdr, "PYMT_DOCNO"),
                                ChqAmt = SafeDec(rdr, "CHQ_AMT"),
                                ChqNo = SafeStr(rdr, "CHQ_NO"),
                                CctName = SafeStr(rdr, "CCT_NAME")
                            });
                        }
                    }
                }
                catch (OracleException oex)
                {
                    throw new Exception($"Oracle error {oex.Number}: {oex.Message}", oex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching cash-book data from database", ex);
                }
            }
            return result;
        }

        private string FormatDateForOracle(string yyyymmdd)
        {
            if (string.IsNullOrWhiteSpace(yyyymmdd) || yyyymmdd.Length != 8) return null;
            return $"{yyyymmdd.Substring(0, 4)}/{yyyymmdd.Substring(4, 2)}/{yyyymmdd.Substring(6, 2)}";
        }

        #region Safe Readers

        private string SafeStr(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? null : r.GetString(ord);
        }

        private decimal? SafeDec(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? (decimal?)null : r.GetDecimal(ord);
        }

        private DateTime? SafeDate(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? (DateTime?)null : r.GetDateTime(ord);
        }

        #endregion Safe Readers
    }
}