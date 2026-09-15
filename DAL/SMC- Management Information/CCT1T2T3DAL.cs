using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class CCT1T2T3DAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CCT1T2T3Model> GetCCT1T2T3(DateTime fromDate, DateTime toDate, string costCtr)
        {
            var result = new List<CCT1T2T3Model>();

            const string query = @"
                SELECT  b.application_no,
                        b.application_id,
                        a.project_no,
                        a.ACC_CREATED_DATE,
                        (SELECT submit_date
                           FROM applications
                          WHERE application_id = b.application_id) AS piv_1_date,
                        c.APPROVED_DATE AS approval_date,
                        d.TOTAL_COST AS estimate_cost,
                        (SELECT CONFIRMED_DATE
                           FROM PIV_DETAIL
                          WHERE reference_no = b.application_no
                            AND reference_type = 'EST'
                            AND status = 'P') AS piv2_date,
                        (SELECT CONNECTED_DATE
                           FROM spodrcrd
                          WHERE PROJECT_NO = a.project_no) AS engized_date,
                        (c.APPROVED_DATE - (SELECT submit_date
                                               FROM applications
                                              WHERE application_id = b.application_id)) AS t1,
                        ((SELECT CONNECTED_DATE
                            FROM spodrcrd
                           WHERE PROJECT_NO = a.project_no) - c.APPROVED_DATE) AS t2_ln,
                        ((SELECT CONNECTED_DATE
                            FROM spodrcrd
                           WHERE PROJECT_NO = a.project_no) -
                         (SELECT CONFIRMED_DATE
                            FROM PIV_DETAIL
                           WHERE reference_no = b.application_no
                             AND reference_type = 'EST'
                             AND status = 'P')) AS t2_smc,
                        (a.ACC_CREATED_DATE - (SELECT CONNECTED_DATE
                                                  FROM spodrcrd
                                                 WHERE PROJECT_NO = a.project_no)) AS t3,
                        (SELECT 'Loan'
                           FROM PIV_DETAIL
                          WHERE reference_no = b.application_no
                            AND reference_type = 'ELN'
                            AND status = 'C') AS loan,
                        (SELECT dept_nm
                           FROM gldeptm
                          WHERE TRIM(dept_id) = TRIM(:costctr)) AS cct_name
                FROM      Spexpjob a, Application_Reference b, approval c, speststd d
                WHERE     TRIM(a.project_no) = TRIM(b.PROJECTNO)
                  AND     a.ACC_CREATED_DATE >= :fromdate
                  AND     a.ACC_CREATED_DATE < :todateexcl
                  AND     TRIM(a.dept_id) = TRIM(:costctr)
                  AND     c.reference_no = b.application_no
                  AND     d.estimate_no = b.application_no
                  AND     d.TOTAL_COST = c.standard_cost
                  AND     c.TO_STATUS = 30
                  AND     b.application_no LIKE '%ENC%'
                ORDER BY a.project_no";

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
                        result.Add(new CCT1T2T3Model
                        {
                            ApplicationNo = reader["application_no"] == DBNull.Value ? null : reader["application_no"].ToString(),
                            ApplicationId = reader["application_id"] == DBNull.Value ? null : reader["application_id"].ToString(),
                            ProjectNo = reader["project_no"] == DBNull.Value ? null : reader["project_no"].ToString(),
                            AccCreatedDate = reader["ACC_CREATED_DATE"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["ACC_CREATED_DATE"]),
                            Piv1Date = reader["piv_1_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv_1_date"]),
                            ApprovalDate = reader["approval_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["approval_date"]),
                            EstimateCost = reader["estimate_cost"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["estimate_cost"]),
                            Piv2Date = reader["piv2_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv2_date"]),
                            EnergizedDate = reader["engized_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["engized_date"]),
                            T1 = reader["t1"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["t1"]),
                            T2Ln = reader["t2_ln"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["t2_ln"]),
                            T2Smc = reader["t2_smc"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["t2_smc"]),
                            T3 = reader["t3"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["t3"]),
                            Loan = reader["loan"] == DBNull.Value ? null : reader["loan"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}