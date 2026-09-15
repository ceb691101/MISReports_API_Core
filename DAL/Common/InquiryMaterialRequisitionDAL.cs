using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class InquiryMaterialRequisitionDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<InquiryMaterialRequisitionModel> GetInquiryMaterialRequisition(string fromDate, string toDate, string costCtr)
        {
            var result = new List<InquiryMaterialRequisitionModel>();

            string query = @"
        SELECT 'Material  Requisition' AS Category, doc_no, doc_pf, req_dt, ent_by, modi_by, apr_uid_1,
               (CASE WHEN status = 4 THEN 'Issue Generated'
                     WHEN status = 6 THEN 'Issue Posting'
                     WHEN status = 7 THEN 'Requisition Confirm '
                     WHEN status = 9 THEN 'Posted Cancellation '
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM mtreqhmt
        WHERE status NOT IN (1, 2, 3, 8)
          AND dept_id = :costctr
          AND req_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND req_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        UNION ALL
        SELECT 'Material Requisition-Temp' AS Category, doc_no, doc_pf, req_dt, ent_by, modi_by, apr_uid_1,
               (CASE WHEN status = 2 THEN 'Send for Approval'
                     WHEN status = 3 THEN 'Rejected'
                     WHEN status = 4 THEN 'Approved '
                     WHEN status = 8 THEN 'Send for Printing '
                     WHEN status = 9 THEN 'Cancelled'
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM mtreqhtt
        WHERE status IN (2, 3, 4, 8, 9)
          AND dept_id = :costctr
          AND req_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND req_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        ORDER BY 8, doc_pf, doc_no, req_dt";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new InquiryMaterialRequisitionModel
                        {
                            Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            ReqDt = reader["req_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["req_dt"]),
                            EntBy = reader["ent_by"] == DBNull.Value ? null : reader["ent_by"].ToString(),
                            ModiBy = reader["modi_by"] == DBNull.Value ? null : reader["modi_by"].ToString(),
                            AprUid1 = reader["apr_uid_1"] == DBNull.Value ? null : reader["apr_uid_1"].ToString(),
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}