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
    public class JobFinBillNotGeneratedDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Overflow-safe string reader, same pattern used on the other reports: some of
        // these columns (estimate_no, contractor_id-backed lookups, etc.) may be NUMBER
        // columns under the hood, and reading them via the plain reader["x"] indexer
        // risks the decimal-overflow exception seen on BulkConnectionDetails if the
        // underlying value has more digits than .NET decimal supports.
        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            Type fieldType = reader.GetFieldType(ordinal);
            if (fieldType == typeof(decimal))
            {
                OracleDecimal od = reader.GetOracleDecimal(ordinal);
                try
                {
                    od = OracleDecimal.SetPrecision(od, 28);
                    return od.Value.ToString();
                }
                catch (OverflowException)
                {
                    return ((double)od).ToString();
                }
            }

            return reader.GetValue(ordinal)?.ToString();
        }

        public List<JobFinBillNotGeneratedModel> GetJobFinBillNotGenerated(DateTime fromDate, DateTime toDate, string costCtr)
        {
            var result = new List<JobFinBillNotGeneratedModel>();

            const string query = @"
                SELECT  P.estimate_no,
                        P.project_no,
                        P.fund_id,
                        P.cat_cd,
                        P.prj_ass_dt,
                        C.finished_date,
                        C.consumer_name,
                        (SELECT contractor_name
                           FROM SPESTCNT
                          WHERE dept_id = C.dept_id
                            AND contractor_id = C.contractor_id) AS Contractor,
                        (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME
                FROM      Pcesthmt P, SPESTCND C
                WHERE     C.status = 'F'
                  AND     TRIM(C.project_no) = TRIM(P.project_no)
                  AND     TRIM(P.dept_id) = TRIM(:costctr)
                  AND     C.finished_date >= :fromdate
                  AND     C.finished_date <  :todateexcl
                ORDER BY P.cat_cd DESC, P.project_no, C.finished_date";

            // Notes vs. the original query:
            // 1. TRIM() added around P.dept_id and gldeptm.dept_id where each is compared
            //    to the :costctr bind variable -- same fix applied across the other
            //    reports, since dept_id is a fixed-length CHAR column elsewhere in this
            //    schema. The contractor subquery's "dept_id = C.dept_id" is a
            //    column-to-column correlation, not a bind variable, so it's unaffected
            //    and was left as-is.
            // 2. Dates are bound as real OracleDbType.Date parameters instead of
            //    TO_DATE(:param,'yyyy/mm/dd') string parsing, and toDate is treated as
            //    inclusive of the whole day (finished_date < toDate + 1 day).
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new JobFinBillNotGeneratedModel
                        {
                            EstimateNo = SafeGetString(reader, "estimate_no"),
                            ProjectNo = SafeGetString(reader, "project_no"),
                            FundId = SafeGetString(reader, "fund_id"),
                            CatCd = SafeGetString(reader, "cat_cd"),
                            PrjAssDt = reader["prj_ass_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["prj_ass_dt"]),
                            FinishedDate = reader["finished_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["finished_date"]),
                            ConsumerName = SafeGetString(reader, "consumer_name"),
                            Contractor = SafeGetString(reader, "Contractor"),
                            CctName = SafeGetString(reader, "CCT_NAME")
                        });
                    }
                }
            }

            return result;
        }
    }
}