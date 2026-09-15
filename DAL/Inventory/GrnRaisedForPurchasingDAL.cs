// DAL/GrnRaisedForPurchasingDAL.cs
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class GrnRaisedForPurchasingDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<GrnRaisedForPurchasingModel> GetGrnRaisedForPurchasing(string fromDate, string toDate, string matCode)
        {
            var result = new List<GrnRaisedForPurchasingModel>();

            const string query = @"
SELECT * FROM (
    SELECT
        T1.MAT_CD,
        T2.MAT_NM,
        SUM(CASE WHEN T1.ADD_DEDUCT = 'F' THEN -T1.TRX_QTY
                 WHEN T1.ADD_DEDUCT = 'T' THEN T1.TRX_QTY
                 ELSE 0.00 END) AS QTY,
        SUM(CASE WHEN T1.ADD_DEDUCT = 'F' THEN -T1.TRX_VAL
                 WHEN T1.ADD_DEDUCT = 'T' THEN T1.TRX_VAL
                 ELSE 0.00 END) AS VALUE
    FROM
        INPOSTMT T1, INMATM T2, INTRHMT T3
    WHERE
        T1.DOC_NO = T3.DOC_NO
        AND T1.DOC_PF = T3.DOC_PF
        AND T1.DEPT_ID = T3.DEPT_ID
        AND T2.MAT_CD = T1.MAT_CD
        AND T3.DEPT_ID IN (
            SELECT DEPT_ID FROM GLDEPTM WHERE COMP_ID IN (
                SELECT COMP_ID FROM GLCOMPM WHERE STATUS = 2
                AND (PARENT_ID LIKE 'DISCO%' OR GRP_COMP LIKE 'DISCO%' OR COMP_ID LIKE 'DISCO%')
            )
        )
        AND T1.TRX_DT >= TO_DATE(:fromdate, 'YYYY/MM/DD')
        AND T1.TRX_DT <= TO_DATE(:todate, 'YYYY/MM/DD')
        AND T1.MAT_CD LIKE :matcode || '%'
        AND T3.RC_FROM = 4
        AND T1.TRX_TYPE IN ('RECEIPT   ', 'RC_CAN')
    GROUP BY
        T1.MAT_CD, T2.MAT_NM
    ORDER BY
        1, 2
) WHERE ROWNUM <= 5000";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });
                // Blank matCode -> LIKE '' || '%' = '%' (matches all). Partial -> 'AB' || '%' = 'AB%'.
                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2) { Value = matCode ?? "" });

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new GrnRaisedForPurchasingModel
                            {
                                MatCd = SafeStr(reader, "MAT_CD"),
                                MatNm = SafeStr(reader, "MAT_NM"),
                                Qty = SafeDec(reader, "QTY"),
                                Value = SafeDec(reader, "VALUE")
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
                    throw new Exception("Error fetching GRN-raised-for-purchasing data from database", ex);
                }
            }

            return result;
        }

        // Period-wide stats — deliberately NOT filtered by matCode (see report note: "* for all materials").
        public GrnPeriodSummaryModel GetPeriodSummary(string fromDate, string toDate)
        {
            const string query = @"
SELECT
    (SELECT COUNT(DOC_NO) FROM INTRHMT
       WHERE RC_FROM = 4
       AND TRX_DT >= TO_DATE(:fromdate, 'YYYY/MM/DD')
       AND TRX_DT <= TO_DATE(:todate, 'YYYY/MM/DD')) AS GRN_COUNT,
    (SELECT COUNT(DOC_NO) FROM INTRHMT
       WHERE (ISSUE_TO IN (1, 5) OR (ISSUE_TO IN (6) AND IS_REF IN ('MAINTENANCE')))
       AND TRX_DT >= TO_DATE(:fromdate, 'YYYY/MM/DD')
       AND TRX_DT <= TO_DATE(:todate, 'YYYY/MM/DD')) AS ISSUE_COUNT,
    (SELECT SUM(CASE WHEN B.ADD_DEDUCT = 'F' THEN B.TRX_VAL
                      WHEN B.ADD_DEDUCT = 'T' THEN -B.TRX_VAL
                      ELSE 0.00 END)
       FROM INTRHMT A, INPOSTMT B
       WHERE (A.ISSUE_TO IN (1, 5) OR (A.ISSUE_TO IN (6) AND A.IS_REF IN ('MAINTENANCE')))
       AND A.TRX_DT >= TO_DATE(:fromdate, 'YYYY/MM/DD')
       AND A.TRX_DT <= TO_DATE(:todate, 'YYYY/MM/DD')
       AND B.TRX_TYPE IN ('ISSUE', 'IS_CAN')
       AND A.DOC_NO = B.DOC_NO AND A.DOC_PF = B.DOC_PF AND A.DEPT_ID = B.DEPT_ID) AS ISSUE_TOTAL
FROM DUAL";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new GrnPeriodSummaryModel
                            {
                                GrnCount = SafeInt(reader, "GRN_COUNT") ?? 0,
                                IssueCount = SafeInt(reader, "ISSUE_COUNT") ?? 0,
                                IssueTotal = SafeDec(reader, "ISSUE_TOTAL") ?? 0m
                            };
                        }
                    }
                }
                catch (OracleException oex)
                {
                    throw new Exception($"Oracle error {oex.Number}: {oex.Message}", oex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching GRN period summary from database", ex);
                }
            }

            return new GrnPeriodSummaryModel { GrnCount = 0, IssueCount = 0, IssueTotal = 0m };
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

        private int? SafeInt(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? (int?)null : Convert.ToInt32(r.GetDecimal(ord));
        }

        #endregion
    }
}