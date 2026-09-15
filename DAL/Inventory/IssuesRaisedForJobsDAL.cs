using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class IssuesRaisedForJobsDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Main method to get all data (optimized)
        public List<IssuesRaisedForJobsModel> GetIssuesRaisedForJobs(
            string fromDate,
            string toDate,
            string matCode)
        {
            var result = new List<IssuesRaisedForJobsModel>();

            // Optimized query without ROWNUM limit
            string query = @"
SELECT
    T1.MAT_CD,
    T2.MAT_NM,
    COUNT(*) AS NO_OF_ISSUES,
    SUM(CASE WHEN T1.ADD_DEDUCT = 'F' THEN T1.TRX_QTY
             WHEN T1.ADD_DEDUCT = 'T' THEN -T1.TRX_QTY
             ELSE 0.00 END) AS QTY
FROM
    INPOSTMT T1
    INNER JOIN INMATM T2 ON T2.MAT_CD = T1.MAT_CD
    INNER JOIN INTRHMT T3 ON T1.DOC_NO = T3.DOC_NO 
        AND T1.DOC_PF = T3.DOC_PF 
        AND T1.DEPT_ID = T3.DEPT_ID
WHERE
    T3.DEPT_ID IN (
        SELECT DEPT_ID FROM GLDEPTM 
        WHERE COMP_ID IN (
            SELECT COMP_ID FROM GLCOMPM 
            WHERE STATUS = 2
            AND (PARENT_ID LIKE 'DISCO%' OR GRP_COMP LIKE 'DISCO%' OR COMP_ID LIKE 'DISCO%')
        )
    )
    AND T1.TRX_DT >= TO_DATE(:fromdate, 'YYYY/MM/DD')
    AND T1.TRX_DT <= TO_DATE(:todate, 'YYYY/MM/DD')
    AND T1.MAT_CD LIKE :matcode || '%'
    AND (T3.ISSUE_TO IN (1, 5) OR (T3.ISSUE_TO IN (6) AND T3.IS_REF IN ('MAINTENANCE')))
    AND T1.TRX_TYPE IN ('ISSUE', 'IS_CAN')
    AND T1.MAT_CD NOT LIKE '%.%'
GROUP BY
    T1.MAT_CD, T2.MAT_NM
ORDER BY
    T1.MAT_CD, T2.MAT_NM";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.CommandTimeout = 300; // 5 minutes timeout

                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });
                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2) { Value = matCode ?? "" });

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new IssuesRaisedForJobsModel
                            {
                                MatCd = SafeStr(reader, "MAT_CD"),
                                MatNm = SafeStr(reader, "MAT_NM"),
                                NoOfIssues = SafeInt(reader, "NO_OF_ISSUES"),
                                Qty = SafeDec(reader, "QTY")
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
                    throw new Exception("Error fetching issues-raised-for-jobs data from database", ex);
                }
            }

            return result;
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