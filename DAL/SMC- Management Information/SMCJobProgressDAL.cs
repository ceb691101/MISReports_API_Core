using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class SMCJobProgressDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<SMCJobProgressModel> GetSmcJobProgress(string fromDate, string toDate, string costCtr)
        {
            var result = new List<SMCJobProgressModel>();
            const string query = @"
                SELECT A.application_no, A.submit_date, C1.approved_date AS approved_date,
                       C.confirmed_date AS piv2_confirmed_date,
                       (SELECT (A1.clear_date - A1.issues_date)
                          FROM spestpem A1
                         WHERE TRIM(A1.application_no) = TRIM(A.application_no)
                           AND A1.dept_id = A.dept_id) AS d_notice_days,
                       L.allocated_date, L.finished_date, E.projectno,
                       (SELECT T2.etimate_dt
                          FROM pcesthtt T2
                         WHERE TRIM(T2.estimate_no) = TRIM(A.application_no)
                           AND A.dept_id = T2.dept_id
                           AND T2.rev_no = 1) AS etimate_dt,
                       (SELECT T2.prj_ass_dt
                          FROM pcesthmt T2
                         WHERE TRIM(T2.project_no) = TRIM(E.projectno)
                           AND A.dept_id = T2.dept_id) AS prj_ass_dt,
                       (SELECT connected_date
                          FROM spodrcrd
                         WHERE TRIM(project_no) = TRIM(E.projectno)) AS engized_date,
                       (SELECT T4.account_no
                          FROM spexpjob T4
                         WHERE TRIM(T4.project_no) = TRIM(E.projectno)
                           AND E.dept_id = T4.dept_id) AS acc_no,
                       (SELECT T4.acc_created_date
                          FROM spexpjob T4
                         WHERE TRIM(T4.project_no) = TRIM(E.projectno)
                           AND E.dept_id = T4.dept_id) AS acc_date,
                       (C1.approved_date - A.submit_date) AS t1,
                       ((SELECT connected_date
                           FROM spodrcrd
                          WHERE TRIM(project_no) = TRIM(E.projectno)) - C.confirmed_date) AS t2_smc,
                       ((SELECT T4.acc_created_date
                           FROM spexpjob T4
                          WHERE TRIM(T4.project_no) = TRIM(E.projectno)
                            AND E.dept_id = T4.dept_id)
                        - (SELECT connected_date
                             FROM spodrcrd
                            WHERE TRIM(project_no) = TRIM(E.projectno))) AS t3,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM applications A, piv_detail C, approval C1,
                     (application_reference E
                        LEFT OUTER JOIN spestcnd L ON TRIM(E.projectno) = TRIM(L.project_no))
                WHERE TRIM(A.application_no) = TRIM(C.reference_no)
                  AND A.application_id = E.application_id
                  AND A.dept_id = E.dept_id
                  AND C.reference_type = 'EST'
                  AND C.status IN ('C', 'P')
                  AND A.dept_id = :costctr
                  AND A.application_no LIKE '%ENC%'
                  AND C1.reference_no = A.application_no
                  AND C1.to_status = 30
                  AND A.submit_date >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND A.submit_date <= TO_DATE(:todate,'yyyy/mm/dd')
                ORDER BY A.application_no";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new SMCJobProgressModel
                        {
                            ApplicationNo = reader["application_no"] == DBNull.Value ? null : reader["application_no"].ToString(),
                            SubmitDate = reader["submit_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["submit_date"]),
                            ApprovedDate = reader["approved_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["approved_date"]),
                            Piv2ConfirmedDate = reader["piv2_confirmed_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv2_confirmed_date"]),
                            DNoticeDays = reader["d_notice_days"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["d_notice_days"]),
                            AllocatedDate = reader["allocated_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["allocated_date"]),
                            FinishedDate = reader["finished_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["finished_date"]),
                            ProjectNo = reader["projectno"] == DBNull.Value ? null : reader["projectno"].ToString(),
                            EstimateDt = reader["etimate_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["etimate_dt"]),
                            PrjAssDt = reader["prj_ass_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["prj_ass_dt"]),
                            EngizedDate = reader["engized_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["engized_date"]),
                            AccNo = reader["acc_no"] == DBNull.Value ? null : reader["acc_no"].ToString(),
                            AccDate = reader["acc_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["acc_date"]),
                            T1 = reader["t1"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["t1"]),
                            T2Smc = reader["t2_smc"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["t2_smc"]),
                            T3 = reader["t3"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["t3"]),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}