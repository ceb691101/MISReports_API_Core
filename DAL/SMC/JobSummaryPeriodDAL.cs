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
    public class JobSummaryPeriodDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            return reader.GetValue(ordinal)?.ToString();
        }
        private static decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            OracleDecimal od = reader.GetOracleDecimal(ordinal);
            try
            {
                od = OracleDecimal.SetPrecision(od, 28);
                return od.Value;
            }
            catch (OverflowException)
            {
                return (decimal)(double)od;
            }
        }

        private static DateTime? SafeGetDate(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            return Convert.ToDateTime(reader.GetValue(ordinal));
        }

        public List<JobSummaryPeriodModel> GetJobSummaryPeriod(DateTime fromDate, DateTime toDate, string costCtr)
        {
            var result = new List<JobSummaryPeriodModel>();

            const string query = @"
                SELECT    T2.PRJ_ASS_DT,
                          T2.PROJECT_NO,
                          T2.estimate_NO,
                          (SELECT T3.TOTAL_COST
                             FROM SPESTSTD T3
                            WHERE TRIM(T3.estimate_no) = TRIM(T2.estimate_NO)
                              AND TRIM(T2.dept_id) = TRIM(T3.dept_id)) AS STANDARD_COST,
                          T2.std_cost AS ESTIMATE_COST,
                          SUM(CASE WHEN T1.add_ded = 'DED' THEN -T1.trx_amt
                                   WHEN T1.add_ded = 'ADD' THEN T1.trx_amt
                                   ELSE NULL
                              END) AS ACTUAL,
                          T2.DESCR,
                          (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME
                FROM      PCESTHMT T2
                LEFT OUTER JOIN PCTRXDMT T1 ON TRIM(T2.PROJECT_NO) = TRIM(T1.PROJECT_NO)
                                            AND TRIM(T2.dept_id) = TRIM(T1.dept_id)
                WHERE     TRIM(T2.dept_id) = TRIM(:costctr)
                  AND     T2.PRJ_ASS_DT >= :fromdate
                  AND     T2.PRJ_ASS_DT <  :todateexcl
                GROUP BY  T2.PROJECT_NO, T2.PRJ_ASS_DT, T2.DESCR, T2.estimate_NO, T2.dept_id, T2.std_cost
                ORDER BY  T2.PROJECT_NO";

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
                        result.Add(new JobSummaryPeriodModel
                        {
                            PrjAssDt = SafeGetDate(reader, "PRJ_ASS_DT"),
                            ProjectNo = SafeGetString(reader, "PROJECT_NO"),
                            EstimateNo = SafeGetString(reader, "ESTIMATE_NO"),
                            StandardCost = SafeGetDecimal(reader, "STANDARD_COST"),
                            EstimateCost = SafeGetDecimal(reader, "ESTIMATE_COST"),
                            Actual = SafeGetDecimal(reader, "ACTUAL"),
                            Descr = SafeGetString(reader, "DESCR"),
                            CctName = SafeGetString(reader, "CCT_NAME")
                        });
                    }
                }
            }

            return result;
        }
    }
}