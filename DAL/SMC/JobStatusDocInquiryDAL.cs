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
    public class JobStatusDocInquiryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

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

        public List<JobStatusDocInquiryModel> GetJobStatusDocInquiry(DateTime fromDate, DateTime toDate, string costCtr, string appSubType)
        {
            var result = new List<JobStatusDocInquiryModel>();

            // Job-side status labels (branch 1: post-estimate/job lifecycle statuses).
            const string jobTranStatusCase = @"
                CASE WHEN a.status = 75 THEN 'Modified'
                     WHEN a.status = 1  THEN 'Job On Going-(New)'
                     WHEN a.status = 3  THEN 'Job Hard Closed'
                     WHEN a.status = 4  THEN 'Job Soft Closed'
                     WHEN a.status = 5  THEN 'Job Revised'
                     WHEN a.status = 6  THEN 'Job Exported'
                     WHEN a.status = 22 THEN 'Job Posted'
                     WHEN a.status = 41 THEN 'Job Rejected'
                     WHEN a.status = 55 THEN 'Job To be Approved by ES'
                     WHEN a.status = 56 THEN 'Job To be Approved by EA'
                     WHEN a.status = 57 THEN 'Job To be Approved by EE'
                     WHEN a.status = 58 THEN 'Job To be Approved by DGM'
                     WHEN a.status = 59 THEN 'Job To be Approved by AGM'
                     WHEN a.status = 60 THEN 'Job Approved'
                     WHEN a.status = 61 THEN 'Job To be Approved by CE'
                     ELSE NULL
                END";

            // Estimate-side status labels (branch 2: pre-job estimate approval statuses).
            const string estimateTranStatusCase = @"
                CASE WHEN a.status = 75 THEN 'Modified'
                     WHEN a.status = 44 THEN 'Estimate To be Approved by ES'
                     WHEN a.status = 45 THEN 'Estimate To be Approved by EA'
                     WHEN a.status = 46 THEN 'Estimate To be Approved by EE'
                     WHEN a.status = 47 THEN 'Estimate To be Approved by CE'
                     WHEN a.status = 48 THEN 'Estimate To be Approved by DGM'
                     WHEN a.status = 49 THEN 'Estimate To be Approved by AGM'
                     WHEN a.status = 31 THEN 'Estimate Rejected'
                     WHEN a.status = 30 THEN 'Estimate Approved'
                     WHEN a.status = 33 THEN 'Job No to be created'
                     WHEN a.status = 22 THEN 'Contractor to be Allocated'
                     ELSE NULL
                END";

            string query = @"
                SELECT   a.status,
                         a.fund_id,
                         c.application_id,
                         c.projectno,
                         a.estimate_no,
                         (SELECT description FROM applicationsubtypes WHERE appsubtype = :appsubtype) AS CAT_CD,
                         a.std_cost,
                         d.total_cost,
                         (SELECT first_name || ' ' || last_name FROM applicant WHERE id_no = a1.id_no) AS NAME,
                         (SELECT street_address || ' ' || suburb || ' ' || city FROM applicant WHERE id_no = a1.id_no) AS ADDRESS,
                         " + jobTranStatusCase + @" AS TRAN_STATUS,
                         (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME
                FROM      pcesthmt a
                JOIN      speststd d ON TRIM(a.estimate_no) = TRIM(d.estimate_no)
                JOIN      APPLICATION_REFERENCE c ON TRIM(a.estimate_no) = TRIM(c.application_no)
                                                  AND TRIM(a.dept_id) = TRIM(c.dept_id)
                JOIN      applications a1 ON TRIM(c.application_no) = TRIM(a1.application_no)
                                         AND TRIM(c.Id_no) = TRIM(a1.Id_no)
                                         AND TRIM(a1.dept_id) = TRIM(c.dept_id)
                WHERE     TRIM(a.dept_id) = TRIM(:costctr)
                  AND     a.etimate_dt >= :fromdate
                  AND     a.etimate_dt <  :todateexcl
                  AND     a.status IN (75, 23, 44, 46, 47, 46, 48, 49, 31, 30, 33, 22, 1, 4, 5, 6)
                  AND     a1.application_sub_type = :appsubtype
                GROUP BY " + jobTranStatusCase + @",
                         a.fund_id, a.estimate_no, a.cat_cd, a.std_cost, d.total_cost, a.status,
                         a1.id_no, c.application_id, c.projectno

                UNION ALL

                SELECT   a.status,
                         a.fund_id,
                         c.application_id,
                         c.projectno,
                         a.estimate_no,
                         (SELECT description FROM applicationsubtypes WHERE appsubtype = :appsubtype) AS CAT_CD,
                         a.std_cost,
                         d.total_cost,
                         (SELECT first_name || ' ' || last_name FROM applicant WHERE id_no = a1.id_no) AS NAME,
                         (SELECT street_address || ' ' || suburb || ' ' || city FROM applicant WHERE id_no = a1.id_no) AS ADDRESS,
                         " + estimateTranStatusCase + @" AS TRAN_STATUS,
                         (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME
                FROM      pcesthmt a
                JOIN      speststd d ON TRIM(a.estimate_no) = TRIM(d.estimate_no)
                JOIN      APPLICATION_REFERENCE c ON TRIM(a.estimate_no) = TRIM(c.application_no)
                                                  AND TRIM(a.dept_id) = TRIM(c.dept_id)
                JOIN      applications a1 ON TRIM(c.application_no) = TRIM(a1.application_no)
                                         AND TRIM(c.Id_no) = TRIM(a1.Id_no)
                                         AND TRIM(a1.dept_id) = TRIM(c.dept_id)
                WHERE     TRIM(a.dept_id) = TRIM(:costctr)
                  AND     a.etimate_dt >= :fromdate
                  AND     a.etimate_dt <  :todateexcl
                  AND     a.status IN (75, 23, 44, 46, 47, 46, 48, 49, 31, 30, 33, 22)
                  AND     a1.application_sub_type = :appsubtype
                GROUP BY a.cat_cd, " + estimateTranStatusCase + @",
                         a.fund_id, a.estimate_no, a.std_cost, d.total_cost, a.status,
                         a1.id_no, c.application_id, c.projectno

                ORDER BY CAT_CD, STATUS, APPLICATION_ID";

            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string appSubTypeTrimmed = (appSubType ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr/:appsubtype/:fromdate/:todateexcl (each used multiple times) bind correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });
                cmd.Parameters.Add(new OracleParameter("appsubtype", OracleDbType.Varchar2) { Value = appSubTypeTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new JobStatusDocInquiryModel
                        {
                            Status = SafeGetString(reader, "STATUS"),
                            FundId = SafeGetString(reader, "FUND_ID"),
                            ApplicationId = SafeGetString(reader, "APPLICATION_ID"),
                            ProjectNo = SafeGetString(reader, "PROJECTNO"),
                            EstimateNo = SafeGetString(reader, "ESTIMATE_NO"),
                            CatCd = SafeGetString(reader, "CAT_CD"),
                            StdCost = SafeGetDecimal(reader, "STD_COST"),
                            TotalCost = SafeGetDecimal(reader, "TOTAL_COST"),
                            Name = SafeGetString(reader, "NAME"),
                            Address = SafeGetString(reader, "ADDRESS"),
                            TranStatus = SafeGetString(reader, "TRAN_STATUS"),
                            CctName = SafeGetString(reader, "CCT_NAME")
                        });
                    }
                }
            }

            return result;
        }

        public List<Dictionary<string, object>> GetApplicationSubTypes()
        {
            var result = new List<Dictionary<string, object>>();
            const string query = "SELECT * FROM APPLICATIONSUBTYPES";
            int maxRetries = 2;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var conn = new OracleConnection(_connectionString))
                    using (var cmd = new OracleCommand(query, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandTimeout = 30; // Add timeout

                        conn.Open();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, object>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string colName = reader.GetName(i);
                                    object val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    row[colName] = val;
                                }
                                result.Add(row);
                            }
                        }
                    }
                    return result; // Success, exit
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    // Log and retry
                    System.Diagnostics.Debug.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
                    System.Threading.Thread.Sleep(1000 * (attempt + 1));
                }
                catch (Exception ex)
                {
                    // Last attempt failed
                    System.Diagnostics.Debug.WriteLine($"Final attempt failed: {ex.Message}\n{ex.StackTrace}");
                    throw new Exception($"Failed to retrieve application sub types: {ex.Message}", ex);
                }
            }

            return result;
        }
    }
}