using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class CCSolarPendingDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CCSolarPendingModel> GetCCSolarPending(DateTime fromDate, DateTime toDate, string costCtr)
        {
            var result = new List<CCSolarPendingModel>();

            const string query = @"
                SELECT DISTINCT
                        a.application_id,
                        a.application_no,
                        a.submit_date,
                        e.projectno,
                        c.piv_date,
                        (CASE
                            WHEN a.application_sub_type IN ('NA') THEN 'Net Accounting'
                            WHEN a.application_sub_type IN ('NM') THEN 'Net Metering'
                            WHEN a.application_sub_type IN ('NP') THEN 'Net Plus'
                            WHEN a.application_sub_type IN ('BA') THEN 'Bulk Net Accounting'
                            WHEN a.application_sub_type IN ('BM') THEN 'Bulk Net Metering'
                            WHEN a.application_sub_type IN ('BP') THEN 'Bulk Net Plus'
                            WHEN a.application_sub_type IN ('AC') THEN 'Net Accounting Conversion'
                            WHEN a.application_sub_type IN ('PC') THEN 'Net Plus Conversion'
                            WHEN a.application_sub_type IN ('NT') THEN 'Net Metering TOU'
                            WHEN a.application_sub_type IN ('AT') THEN 'Net Accounting TOU'
                            WHEN a.application_sub_type IN ('PP') THEN 'Net Plus Plus (With Acoount No.)'
                            WHEN a.application_sub_type IN ('PB') THEN 'Bulk Net Plus Plus'
                            WHEN a.application_sub_type IN ('PN') THEN 'Net Plus Plus (Without Acoount No.)'
                            ELSE NULL
                         END) AS application_sub_type,
                        c.paid_date,
                        (SELECT c2.paid_date
                           FROM piv_detail c2
                          WHERE c2.reference_type = 'EST'
                            AND TRIM(c2.reference_no) = TRIM(a.application_no)
                            AND c2.status IN ('C', 'P', 'T', 'M', 'Y')) AS piv2_paid_date,
                        (SELECT existing_acc_no
                           FROM WIRING_LAND_DETAIL
                          WHERE application_id = a.application_id) AS existing_acc_no,
                        (CASE
                            WHEN c1.status = 33 THEN 'Job No to be created'
                            WHEN c1.status = 22 THEN 'Contractor to be Allocated'
                            ELSE 'Not Energized'
                         END) AS status,
                        (SELECT dept_nm
                           FROM gldeptm
                          WHERE TRIM(dept_id) = TRIM(:costctr)) AS cct_name
                FROM      applications a, piv_detail c, pcesthtt c1, application_reference e
                WHERE     TRIM(a.application_no) = TRIM(c.reference_no)
                  AND     a.application_id = e.application_id
                  AND     a.dept_id = e.dept_id
                  AND     c.reference_type = 'APP'
                  AND     a.application_type IN ('CR')
                  AND     c1.status IN (33, 22)
                  AND     TRIM(e.application_no) = TRIM(c1.estimate_no)
                  AND     a.application_sub_type IN
                          ('NM','NP','NA','BM','BP','BA','NT','AC','PC','NT','PP','PN','PB','BP','BA')
                  AND     c.status IN ('C', 'P', 'T', 'M', 'Y')
                  AND     TRIM(a.dept_id) = TRIM(:costctr)
                  AND     a.submit_date >= :fromdate
                  AND     a.submit_date < :todateexcl
                  AND     TRIM(c1.project_no) NOT IN (SELECT TRIM(PROJECT_NO) FROM spodrcrd)
                ORDER BY e.projectno, a.application_no";

            // a.dept_id / e.dept_id / gldeptm.dept_id are fixed-length CHAR columns
            // (blank-padded in storage). A Varchar2 bind variable forces non-padded
            // comparison semantics, so both sides are wrapped in TRIM() -- same fix
            // already applied to the CurrentAcctBal and CCT1T2T3 reports.
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();

            // toDate is treated as inclusive of the whole day, so the upper bound used in
            // the query is the start of the following day.
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
                        result.Add(new CCSolarPendingModel
                        {
                            ApplicationId = reader["application_id"] == DBNull.Value ? null : reader["application_id"].ToString(),
                            ApplicationNo = reader["application_no"] == DBNull.Value ? null : reader["application_no"].ToString(),
                            SubmitDate = reader["submit_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["submit_date"]),
                            ProjectNo = reader["projectno"] == DBNull.Value ? null : reader["projectno"].ToString(),
                            PivDate = reader["piv_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv_date"]),
                            ApplicationSubType = reader["application_sub_type"] == DBNull.Value ? null : reader["application_sub_type"].ToString(),
                            PaidDate = reader["paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["paid_date"]),
                            Piv2PaidDate = reader["piv2_paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv2_paid_date"]),
                            ExistingAccNo = reader["existing_acc_no"] == DBNull.Value ? null : reader["existing_acc_no"].ToString(),
                            Status = reader["status"] == DBNull.Value ? null : reader["status"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}