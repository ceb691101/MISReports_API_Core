using MISReports_Api.Models.SolarJobs;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.SolarJobs
{
    public class CcApplicationRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<CcApplicationModel>> GetApplicationsAsync(DateTime fromDate, DateTime toDate, string costctr)
        {
            var results = new List<CcApplicationModel>();

            const string sql = @"
SELECT DISTINCT
       a.application_id,
       a.application_no,
       a.submit_date,
       c1.approved_date,
       e.projectno,
       c.piv_date,
       CASE
           WHEN a.application_sub_type = 'NA' THEN 'Net Accounting'
           WHEN a.application_sub_type = 'NM' THEN 'Net Metering'
           WHEN a.application_sub_type = 'NP' THEN 'Net Plus'
           WHEN a.application_sub_type = 'BA' THEN 'Bulk Net Accounting'
           WHEN a.application_sub_type = 'BM' THEN 'Bulk Net Metering'
           WHEN a.application_sub_type = 'BP' THEN 'Bulk Net Plus'
           WHEN a.application_sub_type = 'AC' THEN 'Net Accounting Conversion'
           WHEN a.application_sub_type = 'PC' THEN 'Net Plus Conversion'
           WHEN a.application_sub_type = 'NT' THEN 'Net Metering TOU'
           WHEN a.application_sub_type = 'AT' THEN 'Net Accounting TOU'
           WHEN a.application_sub_type = 'PP' THEN 'Net Plus Plus (With Acoount No.)'
           WHEN a.application_sub_type = 'PB' THEN 'Bulk Net Plus Plus'
           WHEN a.application_sub_type = 'PN' THEN 'Net Plus Plus (Without Acoount No.)'
           WHEN a.application_sub_type = 'RS' THEN 'Solar Religious Purpose'
           ELSE NULL
       END AS application_sub_type,
       c.paid_date,
       (SELECT MAX(c2.paid_date)
          FROM piv_detail c2
         WHERE c2.dept_id = a.dept_id
           AND c2.reference_type = 'EST'
           AND TRIM(c2.reference_no) = TRIM(a.application_no)
           AND c2.status IN ('C', 'P', 'T', 'M', 'Y')) AS piv2_paid_date,
       (SELECT MAX(sd.connected_date)
          FROM spodrcrd sd
         WHERE TRIM(sd.project_no) = TRIM(e.projectno)) AS energized_date,
       (SELECT MAX(wld.existing_acc_no)
          FROM WIRING_LAND_DETAIL wld
         WHERE wld.application_id = a.application_id) AS existing_acc_no,
       d.dept_nm AS cct_name
    FROM applications a
    JOIN piv_detail c
        ON TRIM(a.application_no) = TRIM(c.reference_no)
       AND c.dept_id = a.dept_id
       AND c.reference_type = 'APP'
    LEFT JOIN approval c1
        ON c1.reference_no = a.application_no
       AND c1.dept_id = a.dept_id
    JOIN application_reference e
        ON a.application_id = e.application_id
       AND a.dept_id = e.dept_id
    LEFT JOIN gldeptm d
        ON d.dept_id = a.dept_id
 WHERE a.application_type = 'CR'
   AND a.application_sub_type IN ('NM', 'NP', 'NA', 'BM', 'BP', 'BA', 'NT', 'AC', 'PC', 'PP', 'PN', 'PB', 'RS')
   AND c.status IN ('C', 'P', 'T', 'M', 'Y')
   AND a.dept_id = :costctr
   AND a.submit_date >= :fromDate
   AND a.submit_date < :toDateExclusive
 ORDER BY e.projectno, a.application_no";

            using (var conn = new OracleConnection(connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costctr.Trim();
                    cmd.Parameters.Add("fromDate", OracleDbType.Date).Value = fromDate.Date;
                    cmd.Parameters.Add("toDateExclusive", OracleDbType.Date).Value = toDate.Date.AddDays(1);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            results.Add(new CcApplicationModel
                            {
                                ApplicationId = GetString(reader, "application_id"),
                                ApplicationNo = GetString(reader, "application_no"),
                                SubmitDate = GetDateTime(reader, "submit_date"),
                                ApprovedDate = GetDateTime(reader, "approved_date"),
                                ProjectNo = GetString(reader, "projectno"),
                                PivDate = GetDateTime(reader, "piv_date"),
                                ApplicationSubType = GetString(reader, "application_sub_type"),
                                PaidDate = GetDateTime(reader, "paid_date"),
                                Piv2PaidDate = GetDateTime(reader, "piv2_paid_date"),
                                EnergizedDate = GetDateTime(reader, "energized_date"),
                                ExistingAccNo = GetString(reader, "existing_acc_no"),
                                CctName = GetString(reader, "cct_name")
                            });
                        }
                    }
                }
            }

            return results;
        }

        private static string GetString(OracleDataReader reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? null : value.ToString();
        }

        private static DateTime? GetDateTime(OracleDataReader reader, string columnName)
        {
            var value = reader[columnName];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }
    }
}