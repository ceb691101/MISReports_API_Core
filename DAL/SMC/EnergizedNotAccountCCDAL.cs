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
    public class EnergizedNotAccountCCDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Overflow-safe string reader, same pattern used on the other reports: some of
        // these columns (application_id, etc.) may be NUMBER columns under the hood.
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

        private static DateTime? SafeGetDate(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            return Convert.ToDateTime(reader.GetValue(ordinal));
        }

        public List<EnergizedNotAccountCCModel> GetEnergizedNotAccountCC(DateTime fromDate, DateTime toDate, string costCtr)
        {
            var result = new List<EnergizedNotAccountCCModel>();

            const string query = @"
                SELECT    a.submit_date,
                          a.APPLICATION_ID,
                          (SELECT c1.paid_date
                             FROM piv_detail c1
                            WHERE TRIM(c1.reference_type) = 'APP'
                              AND TRIM(c1.status) IN ('C', 'P')
                              AND c1.Id_no = a.Id_no
                              AND TRIM(a.APPLICATION_ID) = TRIM(c1.reference_no)
                              AND TRIM(c1.dept_id) = TRIM(:costctr)) AS PIV_DATE1,
                          a.application_no,
                          (SELECT c1.paid_date
                             FROM piv_detail c1
                            WHERE TRIM(c1.reference_type) IN ('EST')
                              AND c1.status IN ('C', 'P')
                              AND TRIM(a.application_no) = TRIM(c1.reference_no)) AS PIV_DATE2,
                          (SELECT c1.paid_date
                             FROM piv_detail c1
                            WHERE TRIM(c1.reference_type) IN ('ELN')
                              AND TRIM(c1.status) IN ('C', 'P')
                              AND TRIM(a.application_no) = TRIM(c1.reference_no)
                              AND TRIM(c1.dept_id) = TRIM(:costctr)) AS PIV_DATE21,
                          L.ALLOCATED_DATE,
                          L.project_no,
                          a.is_loan_app,
                          d.meter_no_1,
                          (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME,
                          d.CONNECTED_DATE,
                          (SELECT exported_date
                             FROM SPEXPJOB
                            WHERE account_no IS NULL
                              AND project_no = d.project_no) AS SENT_FOR_BILLING
                FROM      applications a
                JOIN      APPLICATION_REFERENCE c ON TRIM(a.dept_id) = TRIM(c.dept_id)
                                                  AND TRIM(a.application_no) = TRIM(c.application_no)
                                                  AND c.Id_no = a.Id_no
                JOIN      spodrcrd d ON TRIM(c.projectno) = TRIM(d.project_no)
                JOIN      spestcnd L ON TRIM(L.project_no) = TRIM(d.project_no)
                                    AND TRIM(d.dept_id) = TRIM(L.dept_id)
                WHERE     (d.project_no NOT IN (SELECT project_no FROM SPEXPJOB)
                       OR  d.project_no IN (SELECT project_no FROM SPEXPJOB WHERE account_no IS NULL))
                  AND     TRIM(d.dept_id) = TRIM(:costctr)
                  AND     d.project_no LIKE '%SMC%'
                  AND     a.application_type = 'NC'
                  AND     d.connected_date >= :fromdate
                  AND     d.connected_date <  :todateexcl
                ORDER BY  a.APPLICATION_ID";

            // Notes vs. the original Jasper query:
            // 1. $P!{@costctr}, $P!{@fromDate}, $P!{@toDate} (Jasper report parameters)
            //    replaced with real bind variables (:costctr, :fromdate, :todateexcl).
            //    :costctr is referenced four times (three correlated subqueries plus the
            //    main WHERE filter), so BindByName is required.
            // 2. TRIM() added around every dept_id comparison against the :costctr bind
            //    variable (in the PIV_DATE1/PIV_DATE21 subqueries, the CCT_NAME lookup, and
            //    the main WHERE filter), and around the a.dept_id = c.dept_id /
            //    d.dept_id = L.dept_id join conditions, same convention used across the
            //    other reports. PIV_DATE2's subquery has no dept_id filter at all in the
            //    original query - kept exactly as written (matches on application_no only,
            //    across all departments), since that appears intentional (PIV_DATE1 and
            //    PIV_DATE21 are dept-scoped but PIV_DATE2 is not).
            // 3. Comma-join syntax converted to standard "JOIN ... ON ..." syntax. The
            //    original had three separate TRIM(...)=TRIM(...) conditions all effectively
            //    equating C.projectno, L.project_no and d.project_no to each other
            //    (transitively redundant); consolidated into two ON conditions
            //    (c-to-d and d-to-L) that preserve the same equivalence without duplication.
            // 4. The original's "D.dept_id=$P!{@costctr}" and "d.dept_id=$P!{@costctr}"
            //    were the same condition repeated (Oracle identifiers are case-insensitive
            //    unless quoted), and "D.project_no like '%SMC%'" was likewise duplicated;
            //    each kept only once here.
            // 5. Dates are bound as real OracleDbType.Date parameters instead of
            //    TO_DATE(:param,'yyyy/mm/dd') string parsing, and toDate is treated as
            //    inclusive of the whole day (connected_date < toDate + 1 day) instead of
            //    the original "<= toDate", to catch any time component on CONNECTED_DATE.
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used multiple times) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new EnergizedNotAccountCCModel
                        {
                            SubmitDate = SafeGetDate(reader, "SUBMIT_DATE"),
                            ApplicationId = SafeGetString(reader, "APPLICATION_ID"),
                            PivDate1 = SafeGetDate(reader, "PIV_DATE1"),
                            ApplicationNo = SafeGetString(reader, "APPLICATION_NO"),
                            PivDate2 = SafeGetDate(reader, "PIV_DATE2"),
                            PivDate21 = SafeGetDate(reader, "PIV_DATE21"),
                            AllocatedDate = SafeGetDate(reader, "ALLOCATED_DATE"),
                            ProjectNo = SafeGetString(reader, "PROJECT_NO"),
                            IsLoanApp = SafeGetString(reader, "IS_LOAN_APP"),
                            MeterNo1 = SafeGetString(reader, "METER_NO_1"),
                            CctName = SafeGetString(reader, "CCT_NAME"),
                            ConnectedDate = SafeGetDate(reader, "CONNECTED_DATE"),
                            SentForBilling = SafeGetDate(reader, "SENT_FOR_BILLING")
                        });
                    }
                }
            }

            return result;
        }
    }
}