using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using NLog;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class MaterialReqJobwiseNoMatDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public List<MaterialReqJobwiseNoMatModel> GetMaterialReqJobwiseNoMat(string costCtr, string projectNo)
        {
            var result = new List<MaterialReqJobwiseNoMatModel>();
            
            // Clean the input - trim spaces
            costCtr = costCtr?.Trim() ?? string.Empty;
            projectNo = projectNo?.Trim() ?? string.Empty;
            
            Logger.Info($"GetMaterialReqJobwiseNoMat called with costCtr='{costCtr}', projectNo='{projectNo}'");
            System.Diagnostics.Debug.WriteLine($"=== DEBUG: Querying with costCtr: '{costCtr}', projectNo: '{projectNo}' ===");

            string query = @"
        SELECT 'Material  Requisition' AS Category,
               A.doc_no, A.doc_pf, A.req_dt, A.issue_doc_pf, A.issue_doc_no, A.req_source, A.req_cost,
               A.apr_dt_1, A.qty_apr_dt AS apr_dt_2,
               (SELECT DISTINCT T1.trx_dt FROM inpostmt T1, intrhmt T2
                WHERE T1.doc_no = T2.doc_no
                  AND T1.doc_pf = T2.doc_pf
                  AND T1.dept_id = T2.dept_id
                  AND T2.des_dept_id = :costctr
                  AND (T2.issue_to = 1 OR T2.rc_from = 1)
                  AND T2.src_doc_no = A.doc_no
                  AND T2.doc_no = A.issue_doc_no) AS post_dt,
               (CASE WHEN A.status = 2 THEN 'Send for Approval'
                     WHEN A.status = 3 THEN 'Rejected'
                     WHEN A.status = 4 THEN 'Approved '
                     WHEN A.status = 8 THEN 'Send for Printing '
                     WHEN A.status = 9 THEN 'Cancelled'
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM mtreqhmt A
        WHERE TRIM(A.dept_id) = TRIM(:costctr)
          AND TRIM(A.req_source) = TRIM(:projectno)
        ORDER BY doc_pf, doc_no, req_dt";

            try
            {
                using (var conn = new OracleConnection(_connectionString))
                using (var cmd = new OracleCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                    cmd.Parameters.Add(new OracleParameter("projectno", OracleDbType.Varchar2) { Value = projectNo });

                    System.Diagnostics.Debug.WriteLine($"=== Parameter costCtr: '{cmd.Parameters[0].Value}' ===");
                    System.Diagnostics.Debug.WriteLine($"=== Parameter projectNo: '{cmd.Parameters[1].Value}' ===");

                    conn.Open();

                    // DEBUG: Check if records exist with exact match
                    using (var checkCmd = new OracleCommand(
                        "SELECT COUNT(*) FROM mtreqhmt WHERE TRIM(dept_id) = TRIM(:costctr) AND TRIM(req_source) = TRIM(:projectno)", conn))
                    {
                        checkCmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costCtr;
                        checkCmd.Parameters.Add("projectno", OracleDbType.Varchar2).Value = projectNo;
                        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        Logger.Info($"Records found with TRIM match: {count}");
                        System.Diagnostics.Debug.WriteLine($"=== Records in mtreqhmt (TRIM match): {count} ===");

                        // If zero, check what values exist for this costCtr
                        if (count == 0)
                        {
                            Logger.Info($"No records found. Checking existing req_source values for costCtr='{costCtr}'...");
                            
                            using (var sampleCmd = new OracleCommand(
                                "SELECT DISTINCT TRIM(req_source) FROM mtreqhmt WHERE TRIM(dept_id) = TRIM(:costctr) AND ROWNUM <= 10", conn))
                            {
                                sampleCmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costCtr;
                                using (var reader = sampleCmd.ExecuteReader())
                                {
                                    System.Diagnostics.Debug.WriteLine($"=== Sample req_source values for dept_id='{costCtr}': ===");
                                    Logger.Info($"Sample req_source values for dept_id='{costCtr}':");
                                    bool hasSamples = false;
                                    while (reader.Read())
                                    {
                                        hasSamples = true;
                                        var value = reader[0].ToString();
                                        System.Diagnostics.Debug.WriteLine($"  - '{value}'");
                                        Logger.Info($"  - '{value}'");
                                    }
                                    if (!hasSamples)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"  (No records found for this costCtr)");
                                        Logger.Info($"  (No records found for this costCtr)");
                                    }
                                }
                            }
                        }
                    }

                    // Execute main query
                    using (var reader = cmd.ExecuteReader())
                    {
                        int recordCount = 0;
                        while (reader.Read())
                        {
                            recordCount++;
                            result.Add(new MaterialReqJobwiseNoMatModel
                            {
                                Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                                DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                                DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                                ReqDt = reader["req_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["req_dt"]),
                                IssueDocPf = reader["issue_doc_pf"] == DBNull.Value ? null : reader["issue_doc_pf"].ToString(),
                                IssueDocNo = reader["issue_doc_no"] == DBNull.Value ? null : reader["issue_doc_no"].ToString(),
                                ReqSource = reader["req_source"] == DBNull.Value ? null : reader["req_source"].ToString(),
                                ReqCost = reader["req_cost"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["req_cost"]),
                                AprDt1 = reader["apr_dt_1"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["apr_dt_1"]),
                                AprDt2 = reader["apr_dt_2"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["apr_dt_2"]),
                                PostDt = reader["post_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["post_dt"]),
                                TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                                BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                            });
                        }
                        Logger.Info($"Main query returned {recordCount} records");
                        System.Diagnostics.Debug.WriteLine($"=== Main Query Result: {recordCount} records ===");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetMaterialReqJobwiseNoMat: {ex.Message}", ex);
                System.Diagnostics.Debug.WriteLine($"=== ERROR: {ex.Message}\n{ex.StackTrace} ===");
                throw;
            }
            return result;
        }
    }
}