// DAL/IssueReceiptWPDAL.cs
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class IssueReceiptWPDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<IssueReceiptWPModel> GetIssueReceiptWP(string fromDate, string toDate, string costCtr)
        {
            var result = new List<IssueReceiptWPModel>();

            const string query = @"
SELECT * FROM (
    SELECT DISTINCT
        T3.DOC_PF,
        T1.WRH_CD,
        T3.DOC_NO,
        T3.REF_1,
        T3.REF_2,
        T3.REF_3,
        T3.REF_4,
        T1.TRX_DT,
        SUM(T1.TRX_VAL) AS TOTAL,
        T3.DES_DEPT_ID,
        (SELECT DEPT_NM FROM GLDEPTM WHERE DEPT_ID = :costctr) AS CCT_NAME
    FROM
        INPOSTMT T1, INTRHMT T3
    WHERE
        T1.DOC_NO = T3.DOC_NO
        AND T1.DOC_PF = T3.DOC_PF
        AND T1.DEPT_ID = T3.DEPT_ID
        AND T3.DEPT_ID = :costctr
        AND TO_DATE(:fromdate, 'YYYY/MM/DD') <= T1.TRX_DT
        AND TO_DATE(:todate, 'YYYY/MM/DD') >= T1.TRX_DT
        AND T1.TRX_TYPE IN ('ISSUE', 'IS_CAN', 'RECEIPT', 'RC_CAN')
    GROUP BY
        T3.DOC_PF, T1.WRH_CD, T3.DOC_NO, T3.REF_1, T3.REF_2, T3.REF_3, T3.REF_4, T1.TRX_DT, T3.DES_DEPT_ID
    ORDER BY
        T3.DOC_PF, T1.WRH_CD, T3.DOC_NO
) WHERE ROWNUM <= 5000";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new IssueReceiptWPModel
                            {
                                DocPf = SafeStr(reader, "DOC_PF"),
                                WrhCd = SafeStr(reader, "WRH_CD"),
                                DocNo = SafeStr(reader, "DOC_NO"),
                                Ref1 = SafeStr(reader, "REF_1"),
                                Ref2 = SafeStr(reader, "REF_2"),
                                Ref3 = SafeStr(reader, "REF_3"),
                                Ref4 = SafeStr(reader, "REF_4"),
                                TrxDt = SafeDate(reader, "TRX_DT"),
                                Total = SafeDec(reader, "TOTAL"),
                                DesDeptId = SafeStr(reader, "DES_DEPT_ID"),
                                CctName = SafeStr(reader, "CCT_NAME")
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
                    throw new Exception("Error fetching issue-receipt data from database", ex);
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

        private DateTime? SafeDate(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? (DateTime?)null : r.GetDateTime(ord);
        }

        #endregion
    }
}