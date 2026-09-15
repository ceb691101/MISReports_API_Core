using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class CCDocInquiryPendingDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CCDocInquiryPendingModel> GetCCDocInquiryPending(string fromDate, string toDate, string costCtr)
        {
            var result = new List<CCDocInquiryPendingModel>();
            const string query = @"
                SELECT 'Inventory' AS category, A.dept_id, A.doc_no, A.trx_dt AS doc_dt,
                       (CASE WHEN A.status = 1 THEN 'New'
                             WHEN A.status = 2 THEN 'Confirmed Record'
                             WHEN A.status = 3 THEN 'Send for 1st. Approval'
                             WHEN A.status = 4 THEN 'Posted. But not Accounted'
                             WHEN A.status = 5 THEN 'Cancelled  Record'
                             WHEN A.status = 6 THEN 'GL Posted'
                             WHEN A.status = 7 THEN 'First Approval'
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM intrhmt A
                WHERE A.status <> 6
                  AND A.dept_id = :costctr
                  AND A.trx_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND A.trx_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT 'Cash Book- After Posting' AS category, dept_id, doc_no, doc_dt,
                       (CASE WHEN status = 1 THEN 'New'
                             WHEN status = 2 THEN 'Send for Approval'
                             WHEN status = 3 THEN 'Approved'
                             WHEN status = 6 THEN 'To be cancelled'
                             WHEN status = 5 THEN 'Cancelled  Record'
                             WHEN status = 7 THEN 'Payment Plan generated '
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM cbpmthmt
                WHERE NOT (status = 4 OR status = 8)
                  AND dept_id = :costctr
                  AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT 'Cash Book-Temp Transcation' AS category, dept_id, doc_no, doc_dt,
                       (CASE WHEN status = 1 THEN 'New Record'
                             WHEN status = 2 THEN 'Send for 1st. Approval'
                             WHEN status = 3 THEN 'Approved'
                             WHEN status = 4 THEN 'Rejected'
                             WHEN status = 6 THEN 'Send for second approval'
                             WHEN status = 7 THEN 'Approved Once'
                             WHEN status = 8 THEN 'Printed '
                             WHEN status = 9 THEN 'Cancelled'
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM cbpmthtt
                WHERE NOT (status = 0)
                  AND dept_id = :costctr
                  AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT 'Cheque - Temporary Transcation' AS category, dept_id, chq_run AS doc_no, run_dt AS doc_dt,
                       (CASE WHEN status = 1 THEN 'Create Payment Plan'
                             WHEN status = 3 THEN 'Print PP Final report'
                             WHEN status = 4 THEN 'Rejected'
                             WHEN status = 6 THEN 'Send for second approval'
                             WHEN status = 5 THEN 'Send  PP for Approval'
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM cbchqhtt
                WHERE NOT (status = 0)
                  AND dept_id = :costctr
                  AND run_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND run_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT 'Cheque Payment Transcation' AS category, dept_id, chq_run AS doc_no, run_dt AS doc_dt,
                       (CASE WHEN status = 1 THEN 'Approved  Payment Plan'
                             WHEN status = 3 THEN 'Cheque printed'
                             WHEN status = 5 THEN 'Transfer to GL'
                             WHEN status = 7 THEN 'Cheque assignment Report'
                             WHEN status = 8 THEN 'Confirmation'
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM cbchqhmt
                WHERE NOT (status = 6 OR status = 5)
                  AND dept_id = :costctr
                  AND run_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND run_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT 'Material  Requisition' AS category, dept_id, doc_no, req_dt AS doc_dt,
                       (CASE WHEN status = 4 THEN 'Issue Generated'
                             WHEN status = 6 THEN 'Issue Posting'
                             WHEN status = 7 THEN 'Requisition Confirm '
                             WHEN status = 9 THEN 'Posted Cancellation '
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM mtreqhmt
                WHERE status NOT IN (1,2,3,8)
                  AND dept_id = :costctr
                  AND req_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND req_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT 'Material Requisition-Temp' AS category, dept_id, doc_no, req_dt AS doc_dt,
                       (CASE WHEN status = 2 THEN 'Send for Approval'
                             WHEN status = 3 THEN 'Rejected'
                             WHEN status = 4 THEN 'Approved '
                             WHEN status = 8 THEN 'Send for Printing '
                             WHEN status = 9 THEN 'Cancelled'
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM mtreqhtt
                WHERE status IN (2,3,4,8,9)
                  AND dept_id = :costctr
                  AND req_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND req_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT 'GL-After Posting' AS category, A.dept_id, A.doc_no, A.doc_dt,
                       (CASE WHEN A.status = 1 THEN 'New'
                             WHEN A.status = 2 THEN 'Confirmed'
                             WHEN A.status = 6 THEN 'GL Posted'
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM glvochmt A
                WHERE A.status NOT IN (6)
                  AND A.dept_id = :costctr
                  AND A.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND A.doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT 'GL- Temporary Transcations' AS category, A.dept_id, A.doc_no, A.doc_dt,
                       (CASE WHEN A.status = 1 THEN 'New'
                             WHEN A.status = 2 THEN 'Confirmed'
                             WHEN A.status = 3 THEN 'Edit Batch'
                             WHEN A.status = 4 THEN 'Edited Batch'
                             WHEN A.status = 5 THEN 'Generated'
                             WHEN A.status = 6 THEN 'Printed'
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM glvochtt A
                WHERE NOT (A.status = 0)
                  AND A.dept_id = :costctr
                  AND A.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND A.doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')

                UNION ALL

                SELECT UNIQUE 'Project Costing' AS category, T1.dept_id, T1.doc_no, T1.doc_dt,
                       (CASE WHEN T1.status = 1 THEN 'Job not updated'
                             WHEN T1.status = 2 THEN 'Job Updated. But not posted to GL'
                             ELSE NULL END) AS tranStatus,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM pctrxhmt T1, pctrxdmt T3
                WHERE (T1.status = 1 OR T1.status = 2)
                  AND T1.doc_no = T3.doc_no
                  AND T1.doc_pf = T3.doc_pf
                  AND T1.doc_pf <> 'JV_MIS'
                  AND T1.dept_id = :costctr
                  AND T1.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND T1.doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
                  AND T1.doc_no NOT IN (SELECT T2.doc_no FROM glvochmt T2 WHERE T2.dept_id = :costctr)

                ORDER BY 1, 2, 4";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr/:fromdate/:todate (each reused across all 10 branches) bind correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CCDocInquiryPendingModel
                        {
                            Category = reader["category"] == DBNull.Value ? null : reader["category"].ToString(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}