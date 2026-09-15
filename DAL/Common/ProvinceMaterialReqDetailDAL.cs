using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class ProvinceMaterialReqDetailDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvinceMaterialReqDetailModel> GetProvinceMaterialReqDetail(string fromDate, string toDate, string compId)
        {
            var result = new List<ProvinceMaterialReqDetailModel>();

            string query = @"
        SELECT 'Material  Requisition' AS Category, dept_id, doc_no, doc_pf, req_dt, ent_by, modi_by, apr_uid_1,
               (CASE WHEN status = 1 THEN 'Approved '
                     WHEN status = 2 THEN 'Approved for Issued Return'
                     WHEN status = 3 THEN 'Requested Approved'
                     WHEN status = 8 THEN 'Transfer to GL'
                     WHEN status = 4 THEN 'Issue Generated'
                     WHEN status = 6 THEN 'Issue Posting'
                     WHEN status = 7 THEN 'Requisition Confirm '
                     WHEN status = 9 THEN 'Posted Cancellation '
                     ELSE NULL END) AS tranStatus,
               (SELECT comp_nm FROM glcompm WHERE comp_id = :compid) AS cct_name
        FROM mtreqhmt
        WHERE dept_id IN (
              SELECT dept_id FROM gldeptm
              WHERE comp_id IN (
                  SELECT comp_id FROM glcompm
                  WHERE comp_id = :compid OR parent_id = :compid
              )
          )
          AND req_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND req_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        UNION ALL
        SELECT 'Material Requisition-Temp' AS Category, dept_id, doc_no, doc_pf, req_dt, ent_by, modi_by, apr_uid_1,
               (CASE WHEN status = 2 THEN 'Send for Approval'
                     WHEN status = 3 THEN 'Rejected'
                     WHEN status = 4 THEN 'Approved '
                     WHEN status = 8 THEN 'Send for Printing '
                     WHEN status = 9 THEN 'Cancelled'
                     ELSE NULL END) AS tranStatus,
               (SELECT comp_nm FROM glcompm WHERE comp_id = :compid) AS cct_name
        FROM mtreqhtt
        WHERE status IN (2, 3, 4, 8, 9)
          AND dept_id IN (
              SELECT dept_id FROM gldeptm
              WHERE comp_id IN (
                  SELECT comp_id FROM glcompm
                  WHERE comp_id = :compid OR parent_id = :compid
              )
          )
          AND req_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND req_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        ORDER BY 1, 9, doc_pf, doc_no, req_dt";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compId });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ProvinceMaterialReqDetailModel
                        {
                            Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            ReqDt = reader["req_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["req_dt"]),
                            EntBy = reader["ent_by"] == DBNull.Value ? null : reader["ent_by"].ToString(),
                            ModiBy = reader["modi_by"] == DBNull.Value ? null : reader["modi_by"].ToString(),
                            AprUid1 = reader["apr_uid_1"] == DBNull.Value ? null : reader["apr_uid_1"].ToString(),
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                            BranchName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}