using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class ProvinceT1T2T3DAL
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvinceT1T2T3Model> GetProvinceT1T2T3(DateTime fromDate, DateTime toDate, string compId)
        {
            var result = new List<ProvinceT1T2T3Model>();

            const string query = @"
                SELECT  b.application_no,
                        b.application_id,
                        a.project_no,
                        a.ACC_CREATED_DATE,

                        (SELECT MAX(submit_date)
                           FROM applications
                          WHERE application_id = b.application_id) AS piv_1_date,

                        c.APPROVED_DATE AS approval_date,

                        d.TOTAL_COST AS estimate_cost,

                        (SELECT MAX(CONFIRMED_DATE)
                           FROM PIV_DETAIL
                          WHERE reference_no = b.application_no
                            AND reference_type = 'EST'
                            AND status = 'P') AS piv2_date,

                        (SELECT MAX(CONNECTED_DATE)
                           FROM spodrcrd
                          WHERE PROJECT_NO = a.project_no) AS engized_date,

                        (c.APPROVED_DATE -
                         (SELECT MAX(submit_date)
                            FROM applications
                           WHERE application_id = b.application_id)) AS t1,

                        ((SELECT MAX(CONNECTED_DATE)
                            FROM spodrcrd
                           WHERE PROJECT_NO = a.project_no) -
                         c.APPROVED_DATE) AS t2_ln,

                        ((SELECT MAX(CONNECTED_DATE)
                            FROM spodrcrd
                           WHERE PROJECT_NO = a.project_no) -
                         (SELECT MAX(CONFIRMED_DATE)
                            FROM PIV_DETAIL
                           WHERE reference_no = b.application_no
                             AND reference_type = 'EST'
                             AND status = 'P')) AS t2_smc,

                        (a.ACC_CREATED_DATE -
                         (SELECT MAX(CONNECTED_DATE)
                            FROM spodrcrd
                           WHERE PROJECT_NO = a.project_no)) AS t3,

                        (CASE
                            WHEN EXISTS (
                                SELECT 1
                                  FROM PIV_DETAIL
                                 WHERE reference_no = b.application_no
                                   AND reference_type = 'ELN'
                                   AND status = 'C'
                            )
                            THEN 'Loan'
                         END) AS loan,

                        (SELECT MAX(comp_nm)
                           FROM glcompm
                          WHERE TRIM(comp_id) = TRIM(:compid)) AS comp_name

                FROM Spexpjob a,
                     Application_Reference b,
                     approval c,
                     speststd d

                WHERE TRIM(a.project_no) = TRIM(b.PROJECTNO)

                  AND a.ACC_CREATED_DATE >= :fromdate

                  AND a.ACC_CREATED_DATE < :todateexcl

                  AND EXISTS (
                        SELECT 1
                          FROM gldeptm g
                         WHERE TRIM(g.dept_id) = TRIM(a.dept_id)
                           AND EXISTS (
                                 SELECT 1
                                   FROM glcompm p
                                  WHERE TRIM(p.comp_id) = TRIM(g.comp_id)
                                    AND (   TRIM(p.comp_id)   = TRIM(:compid)
                                         OR TRIM(p.parent_id) = TRIM(:compid))
                           )
                  )

                  AND c.reference_no = b.application_no

                  AND d.estimate_no = b.application_no

                  AND d.TOTAL_COST = c.standard_cost

                  AND c.TO_STATUS = 30

                  AND b.application_no LIKE '%ENC%'

                ORDER BY a.project_no";

            string compIdTrimmed = (compId ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.CommandTimeout = 120;

                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compIdTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                try
                {
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        int ordApplicationNo = reader.GetOrdinal("application_no");
                        int ordApplicationId = reader.GetOrdinal("application_id");
                        int ordProjectNo = reader.GetOrdinal("project_no");
                        int ordAccCreatedDate = reader.GetOrdinal("ACC_CREATED_DATE");
                        int ordPiv1Date = reader.GetOrdinal("piv_1_date");
                        int ordApprovalDate = reader.GetOrdinal("approval_date");
                        int ordEstimateCost = reader.GetOrdinal("estimate_cost");
                        int ordPiv2Date = reader.GetOrdinal("piv2_date");
                        int ordEngizedDate = reader.GetOrdinal("engized_date");
                        int ordT1 = reader.GetOrdinal("t1");
                        int ordT2Ln = reader.GetOrdinal("t2_ln");
                        int ordT2Smc = reader.GetOrdinal("t2_smc");
                        int ordT3 = reader.GetOrdinal("t3");
                        int ordLoan = reader.GetOrdinal("loan");
                        int ordCompName = reader.GetOrdinal("comp_name");

                        while (reader.Read())
                        {
                            string applicationNoForLog = reader.IsDBNull(ordApplicationNo) ? "(null)" : reader.GetString(ordApplicationNo);

                            result.Add(new ProvinceT1T2T3Model
                            {
                                ApplicationNo = reader.IsDBNull(ordApplicationNo) ? null : reader.GetString(ordApplicationNo),
                                ApplicationId = reader.IsDBNull(ordApplicationId) ? null : reader.GetString(ordApplicationId),
                                ProjectNo = reader.IsDBNull(ordProjectNo) ? null : reader.GetString(ordProjectNo),
                                AccCreatedDate = reader.IsDBNull(ordAccCreatedDate) ? (DateTime?)null : reader.GetDateTime(ordAccCreatedDate),
                                Piv1Date = reader.IsDBNull(ordPiv1Date) ? (DateTime?)null : reader.GetDateTime(ordPiv1Date),
                                ApprovalDate = reader.IsDBNull(ordApprovalDate) ? (DateTime?)null : reader.GetDateTime(ordApprovalDate),
                                EstimateCost = SafeGetDecimal(reader, ordEstimateCost, "estimate_cost", applicationNoForLog),
                                Piv2Date = reader.IsDBNull(ordPiv2Date) ? (DateTime?)null : reader.GetDateTime(ordPiv2Date),
                                EnergizedDate = reader.IsDBNull(ordEngizedDate) ? (DateTime?)null : reader.GetDateTime(ordEngizedDate),
                                T1 = SafeGetDecimal(reader, ordT1, "t1", applicationNoForLog),
                                T2Ln = SafeGetDecimal(reader, ordT2Ln, "t2_ln", applicationNoForLog),
                                T2Smc = SafeGetDecimal(reader, ordT2Smc, "t2_smc", applicationNoForLog),
                                T3 = SafeGetDecimal(reader, ordT3, "t3", applicationNoForLog),
                                Loan = reader.IsDBNull(ordLoan) ? null : reader.GetString(ordLoan),
                                CompName = reader.IsDBNull(ordCompName) ? null : reader.GetString(ordCompName)
                            });
                        }
                    }
                }
                catch (OracleException ex)
                {
                    logger.Error(ex, "GetProvinceT1T2T3 failed (exception rethrown). fromDate={fromDate}, toDate={toDate}, compId={compId}", fromDate, toDate, compId);
                    System.Diagnostics.Trace.WriteLine($"Oracle error {ex.Number}: {ex.Message}");
                    throw;
                }
            }

            return result;
        }

        private static decimal? SafeGetDecimal(OracleDataReader reader, int ordinal, string columnName, string applicationNoForLog)
        {
            if (reader.IsDBNull(ordinal))
                return null;

            OracleDecimal oracleVal = reader.GetOracleDecimal(ordinal);

            try
            {
                // Clamp to a precision/scale System.Decimal can safely hold before converting.
                OracleDecimal rounded = OracleDecimal.SetPrecision(oracleVal, 28);
                return (decimal)rounded;
            }
            catch (OverflowException)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[ProvinceT1T2T3DAL] Overflow converting column '{columnName}' for application_no '{applicationNoForLog}'. " +
                    $"Raw Oracle value: {oracleVal.ToString()}");
                return null; // swap for decimal.MaxValue or a sentinel if you'd rather flag than null it out
            }
        }
    }
}