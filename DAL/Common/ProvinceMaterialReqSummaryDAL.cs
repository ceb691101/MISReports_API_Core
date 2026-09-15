using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class ProvinceMaterialReqSummaryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvinceMaterialReqSummaryModel> GetProvinceMaterialReqSummary(string fromDate, string toDate, string compId)
        {
            var result = new List<ProvinceMaterialReqSummaryModel>();

            string query = @"
        SELECT
               (CASE WHEN status = 1 THEN '1-Approved'
                     WHEN status = 2 THEN '2-Approved for Issued Return'
                     WHEN status = 3 THEN '3-Requested Approved'
                     WHEN status = 8 THEN '8-Transfer to GL'
                     WHEN status = 4 THEN '4-Issue Generated'
                     WHEN status = 6 THEN '6-Issue Posting'
                     WHEN status = 7 THEN '7-Requisition Confirm '
                     WHEN status = 9 THEN '9-Posted Cancellation '
                     ELSE NULL END) AS tranStatus,
               dept_id, COUNT(dept_id) AS no_of_documents,
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
        GROUP BY
               (CASE WHEN status = 1 THEN '1-Approved'
                     WHEN status = 2 THEN '2-Approved for Issued Return'
                     WHEN status = 3 THEN '3-Requested Approved'
                     WHEN status = 8 THEN '8-Transfer to GL'
                     WHEN status = 4 THEN '4-Issue Generated'
                     WHEN status = 6 THEN '6-Issue Posting'
                     WHEN status = 7 THEN '7-Requisition Confirm '
                     WHEN status = 9 THEN '9-Posted Cancellation '
                     ELSE NULL END), dept_id
        ORDER BY 1, 2";

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
                        result.Add(new ProvinceMaterialReqSummaryModel
                        {
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            NoOfDocuments = reader["no_of_documents"] == DBNull.Value ? 0 : Convert.ToInt32(reader["no_of_documents"]),
                            BranchName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}