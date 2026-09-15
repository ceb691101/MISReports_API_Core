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
    public class EnergizeAgeAnalysisDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            return reader.GetValue(ordinal)?.ToString();
        }

        // Overflow-safe int reader for the COUNT(...) columns, same overflow-guard pattern
        // used for decimal columns on the other reports.
        private static int SafeGetInt(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return 0;

            OracleDecimal od = reader.GetOracleDecimal(ordinal);
            try
            {
                od = OracleDecimal.SetPrecision(od, 28);
                return decimal.ToInt32(od.Value);
            }
            catch (OverflowException)
            {
                return (int)(double)od;
            }
        }

        public List<EnergizeAgeAnalysisModel> GetEnergizeAgeAnalysis(DateTime fromDate, DateTime toDate, string costCtr)
        {
            var result = new List<EnergizeAgeAnalysisModel>();

            const string periodCase = @"
                CASE WHEN (L.allocated_date - c.confirmed_date) <= 7  THEN '1. One Week'
                     WHEN (L.allocated_date - c.confirmed_date) <= 14 THEN '2. Two Weeks'
                     WHEN (L.allocated_date - c.confirmed_date) <= 21 THEN '3. Three Weeks'
                     WHEN (L.allocated_date - c.confirmed_date) <= 31 THEN '4. One Month'
                     WHEN (L.allocated_date - c.confirmed_date) <= 60 THEN '5. Two Months'
                     WHEN (L.allocated_date - c.confirmed_date) <= 90 THEN '6. Three Months'
                     ELSE '7. More than Three Months'
                END";

            string query = @"
                SELECT    " + periodCase + @" AS PERIOD,
                          COUNT(T1.project_no) AS SUM_COUNT,
                          (SELECT COUNT(c1.piv_no)
                             FROM piv_detail c1
                            WHERE TRIM(c1.reference_type) = 'EST'
                              AND TRIM(c1.status) IN ('C', 'P')
                              AND c1.confirmed_date >= :fromdate
                              AND c1.confirmed_date <  :todateexcl
                              AND TRIM(c1.dept_id) = TRIM(:costctr)) AS NO_OF_JOBS,
                          (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME
                FROM      applications a
                JOIN      piv_detail c ON TRIM(a.application_no) = TRIM(c.reference_no)
                                      AND TRIM(c.Id_no) = TRIM(a.Id_no)
                                      AND TRIM(a.dept_id) = TRIM(c.dept_id)
                JOIN      pcesthmt T1 ON TRIM(T1.dept_id) = TRIM(c.dept_id)
                JOIN      spestcnd L ON TRIM(T1.project_no) = TRIM(L.project_no)
                WHERE     TRIM(c.reference_type) = 'EST'
                  AND     TRIM(c.status) IN ('C', 'P')
                  AND     c.confirmed_date >= :fromdate
                  AND     c.confirmed_date <  :todateexcl
                  AND     TRIM(a.dept_id) = TRIM(:costctr)
                  AND     TRIM(L.project_no) =
                          (SELECT L1.projectno
                             FROM application_reference L1
                            WHERE TRIM(L1.dept_id) = TRIM(:costctr)
                              AND TRIM(L1.application_no) = TRIM(c.reference_no)
                              AND TRIM(c.dept_id) = TRIM(:costctr))
                GROUP BY  " + periodCase + @"
                ORDER BY  1 ASC";

            // Notes vs. the original Jasper query:
            // 1. $P!{@costctr}, $P!{@fromDate}, $P!{@toDate} (Jasper report parameters)
            //    replaced with real bind variables (:costctr, :fromdate, :todateexcl).
            //    :costctr is referenced five times (NO_OF_JOBS subquery, CCT_NAME lookup,
            //    main WHERE filter, and twice inside the correlated project-no subquery),
            //    and the date binds are each used twice, so BindByName is required.
            // 2. TRIM() added around every dept_id/id_no/application_no/project_no
            //    comparison, including ones the original left untrimmed
            //    (T1.dept_id = c.dept_id), matching the convention used across the other
            //    reports since these are fixed-length CHAR columns elsewhere in this schema.
            // 3. Comma-join syntax converted to standard "JOIN ... ON ..." syntax for the
            //    four main tables. The project_no-resolution correlated subquery
            //    (matching L.project_no against application_reference) is kept as a WHERE
            //    condition rather than a JOIN, since it's an equality-to-scalar-subquery
            //    check, not a join to another row source.
            // 4. Dates are bound as real OracleDbType.Date parameters instead of
            //    TO_DATE(:param,'yyyy/mm/dd') string parsing, and toDate is treated as
            //    inclusive of the whole day (confirmed_date < toDate + 1 day) instead of
            //    the original "<= toDate", applied consistently to both the outer query's
            //    date filter and the NO_OF_JOBS subquery's date filter.
            // 5. The CASE expression that buckets (allocated_date - confirmed_date) into
            //    age periods is defined once (periodCase) and reused for both the SELECT
            //    and GROUP BY, to guarantee they stay in sync - functionally identical to
            //    the original's duplicated CASE blocks.
            // 6. COUNT(T1.project_no) aliased as SUM_COUNT (not SUM) since SUM is a
            //    reserved SQL function name; the reader still maps it to the model's Sum
            //    property.
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr/:fromdate/:todateexcl (each used multiple times) bind correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new EnergizeAgeAnalysisModel
                        {
                            Period = SafeGetString(reader, "PERIOD"),
                            Sum = SafeGetInt(reader, "SUM_COUNT"),
                            NoOfJobs = SafeGetInt(reader, "NO_OF_JOBS"),
                            CctName = SafeGetString(reader, "CCT_NAME")
                        });
                    }
                }
            }

            return result;
        }
    }
}