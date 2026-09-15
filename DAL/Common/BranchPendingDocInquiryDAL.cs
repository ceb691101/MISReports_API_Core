using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class BranchPendingDocInquiryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<BranchPendingDocInquiryModel> GetBranchPendingDocInquiry(string fromDate, string toDate, string compId)
        {
            var result = new List<BranchPendingDocInquiryModel>();
            const string query = @"
                SELECT 'Inventory' AS category, A.dept_id, A.doc_pf, A.doc_no, A.trx_dt AS doc_dt,
                       (CASE WHEN A.status = 1 THEN 'New'
                             WHEN A.status = 2 THEN 'Confirmed Record'
                             WHEN A.status = 3 THEN 'Send for 1st. Approval'
                             WHEN A.status = 4 THEN 'Posted. But not Accounted'
                             WHEN A.status = 5 THEN 'Cancelled  Record'
                             WHEN A.status = 6 THEN 'GL Posted'
                             WHEN A.status = 7 THEN 'First Approval'
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM intrhmt A
                WHERE A.status <> 6
                  AND A.dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND A.trx_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND A.trx_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT 'Cash Book- After Posting' AS category, dept_id, doc_pf, doc_no, doc_dt,
                       (CASE WHEN status = 1 THEN 'New'
                             WHEN status = 2 THEN 'Send for Approval'
                             WHEN status = 3 THEN 'Approved'
                             WHEN status = 6 THEN 'To be cancelled'
                             WHEN status = 5 THEN 'Cancelled  Record'
                             WHEN status = 7 THEN 'Payment Plan generated '
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM cbpmthmt
                WHERE NOT (status = 4 OR status = 8)
                  AND dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND doc_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT 'Cash Book-Temp Transcation' AS category, dept_id, doc_pf, doc_no, doc_dt,
                       (CASE WHEN status = 1 THEN 'New Record'
                             WHEN status = 2 THEN 'Send for 1st. Approval'
                             WHEN status = 3 THEN 'Approved'
                             WHEN status = 4 THEN 'Rejected'
                             WHEN status = 6 THEN 'Send for second approval'
                             WHEN status = 7 THEN 'Approved Once'
                             WHEN status = 8 THEN 'Printed '
                             WHEN status = 9 THEN 'Cancelled'
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM cbpmthtt
                WHERE NOT (status = 0)
                  AND dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND doc_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT 'Cheque - Temporary Transcation' AS category, dept_id, doc_pf, chq_run AS doc_no, run_dt AS doc_dt,
                       (CASE WHEN status = 1 THEN 'Create Payment Plan'
                             WHEN status = 3 THEN 'Print PP Final report'
                             WHEN status = 4 THEN 'Rejected'
                             WHEN status = 6 THEN 'Send for second approval'
                             WHEN status = 5 THEN 'Send  PP for Approval'
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM cbchqhtt
                WHERE NOT (status = 0)
                  AND dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND run_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND run_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT 'Cheque Payment Transcation' AS category, dept_id, doc_pf, chq_run AS doc_no, run_dt AS doc_dt,
                       (CASE WHEN status = 1 THEN 'Approved  Payment Plan'
                             WHEN status = 3 THEN 'Cheque printed'
                             WHEN status = 5 THEN 'Transfer to GL'
                             WHEN status = 7 THEN 'Cheque assignment Report'
                             WHEN status = 8 THEN 'Confirmation'
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM cbchqhmt
                WHERE NOT (status = 6 OR status = 5)
                  AND dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND run_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND run_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT 'Material  Requisition' AS category, dept_id, doc_pf, doc_no, req_dt AS doc_dt,
                       (CASE WHEN status = 4 THEN 'Issue Generated'
                             WHEN status = 6 THEN 'Issue Posting'
                             WHEN status = 7 THEN 'Requisition Confirm '
                             WHEN status = 9 THEN 'Posted Cancellation '
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM mtreqhmt
                WHERE status NOT IN (1,2,3,8)
                  AND dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND req_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND req_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT 'Material Requisition-Temp' AS category, dept_id, doc_pf, doc_no, req_dt AS doc_dt,
                       (CASE WHEN status = 2 THEN 'Send for Approval'
                             WHEN status = 3 THEN 'Rejected'
                             WHEN status = 4 THEN 'Approved '
                             WHEN status = 8 THEN 'Send for Printing '
                             WHEN status = 9 THEN 'Cancelled'
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM mtreqhtt
                WHERE status IN (2,3,4,8,9)
                  AND dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND req_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND req_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT 'GL-After Posting' AS category, A.dept_id, A.doc_pf, A.doc_no, A.doc_dt,
                       (CASE WHEN A.status = 1 THEN 'New'
                             WHEN A.status = 2 THEN 'Confirmed'
                             WHEN A.status = 6 THEN 'GL Posted'
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM glvochmt A
                WHERE A.status NOT IN (6)
                  AND A.dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND A.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND A.doc_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT 'GL- Temporary Transcations' AS category, A.dept_id, A.doc_pf, A.doc_no, A.doc_dt,
                       (CASE WHEN A.status = 1 THEN 'New'
                             WHEN A.status = 2 THEN 'Confirmed'
                             WHEN A.status = 3 THEN 'Edit Batch'
                             WHEN A.status = 4 THEN 'Edited Batch'
                             WHEN A.status = 5 THEN 'Generated'
                             WHEN A.status = 6 THEN 'Printed'
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM glvochtt A
                WHERE NOT (A.status = 0)
                  AND A.dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND A.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND A.doc_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1

                UNION ALL

                SELECT UNIQUE 'Project Costing' AS category, T1.dept_id, T1.doc_pf, T1.doc_no, T1.doc_dt,
                       (CASE WHEN T1.status = 1 THEN 'Job not updated'
                             WHEN T1.status = 2 THEN 'Job Updated. But not posted to GL'
                             ELSE NULL END) AS tranStatus,
                       (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = :compid) AS COMP_NM
                FROM pctrxhmt T1, pctrxdmt T3
                WHERE (T1.status = 1 OR T1.status = 2)
                  AND T1.doc_no = T3.doc_no
                  AND T1.doc_pf = T3.doc_pf
                  AND T1.doc_pf <> 'JV_MIS'
                  AND T1.dept_id IN (SELECT dept_id FROM gldeptm WHERE status = 2 AND (TRIM(comp_id) = :compid OR TRIM(comp_id) IN
                        (SELECT TRIM(comp_id) FROM glcompm WHERE TRIM(comp_id) = :compid OR TRIM(parent_id) = :compid OR TRIM(grp_comp) = :compid)))
                  AND T1.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND T1.doc_dt < TO_DATE(:todate,'yyyy/mm/dd') + 1
                  AND T1.doc_no NOT IN (SELECT T2.doc_no FROM glvochmt T2 WHERE T2.dept_id = T1.dept_id)

                ORDER BY 1, 2, 5";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :compid/:fromdate/:todate (each reused many times across 10 branches) bind correctly by name
                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compId });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new BranchPendingDocInquiryModel
                        {
                            Category = reader["category"] == DBNull.Value ? null : reader["category"].ToString().Trim(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString().Trim(),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString().Trim(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString().Trim(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString().Trim(),
                            CompNm = reader["COMP_NM"] == DBNull.Value ? null : reader["COMP_NM"].ToString().Trim()
                        });
                    }
                }
            }
            return result;
        }
    }
}