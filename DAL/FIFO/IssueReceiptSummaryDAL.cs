using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class IssueReceiptSummaryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<IssueReceiptSummaryModel> GetIssueReceiptSummary(string fromDate, string toDate, string costCtr)
        {
            var result = new List<IssueReceiptSummaryModel>();

            const string query = @"
                SELECT 'GRN' AS category,
                       B.batch_id AS doc_no,
                       B.batch_dt AS trx_dt,
                       SUM(B.unit_price * B.qty) AS trx_val,
                       (CASE
                            WHEN A.status = 1 THEN 'New'
                            WHEN A.status = 2 THEN 'Confirmed Record'
                            WHEN A.status = 3 THEN 'Send for 1st. Approval'
                            WHEN A.status = 4 THEN 'Posted'
                            WHEN A.status = 5 THEN 'Cancelled  Record'
                            WHEN A.status = 7 THEN 'GL Posted'
                            ELSE NULL
                        END) AS transtatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM intrhmt A, inpodmt B
                WHERE A.dept_id = :costctr
                  AND B.batch_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                  AND B.batch_dt <= TO_DATE(:todate, 'yyyy/mm/dd')
                  AND A.doc_no = B.batch_id
                  AND A.dept_id = B.dept_id
                GROUP BY B.batch_id, B.batch_dt, A.status

                UNION ALL

                SELECT 'ISSUE' AS category,
                       B.doc_no,
                       A.trx_dt AS trx_dt,
                       SUM(B.mat_val) AS trx_val,
                       (CASE
                            WHEN A.status = 1 THEN 'New'
                            WHEN A.status = 2 THEN 'Confirmed Record'
                            WHEN A.status = 3 THEN 'Send for 1st. Approval'
                            WHEN A.status = 4 THEN 'Posted'
                            WHEN A.status = 5 THEN 'Cancelled  Record'
                            WHEN A.status = 7 THEN 'GL Posted'
                            ELSE NULL
                        END) AS transtatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM intrhmt A, inissdmt B
                WHERE A.dept_id = :costctr
                  AND A.trx_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                  AND A.trx_dt <= TO_DATE(:todate, 'yyyy/mm/dd')
                  AND A.doc_no = B.doc_no
                  AND A.dept_id = B.dept_id
                GROUP BY B.doc_no, A.trx_dt, A.status

                ORDER BY 2, 3";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used 4x) / :fromdate / :todate (2x each) bind correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new IssueReceiptSummaryModel
                        {
                            Category = reader["category"] == DBNull.Value ? null : reader["category"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            TrxDt = reader["trx_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["trx_dt"]),
                            TrxVal = GetSafeDecimal(reader, "trx_val"),
                            TranStatus = reader["transtatus"] == DBNull.Value ? null : reader["transtatus"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Safely reads a NUMBER column as a nullable decimal.
        /// Oracle's NUMBER type (especially SUM() aggregates) can carry more
        /// precision than .NET's decimal can hold, which makes a plain
        /// Convert.ToDecimal(reader[...]) throw OverflowException on some rows.
        /// Reading via OracleDecimal and clamping precision first avoids that.
        /// </summary>
        private static decimal? GetSafeDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;

            OracleDecimal oraVal = reader.GetOracleDecimal(ordinal);
            oraVal = OracleDecimal.SetPrecision(oraVal, 28);

            return oraVal.Value;
        }
    }
}