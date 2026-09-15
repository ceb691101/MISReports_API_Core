using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using NLog;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class MaterialReqJobwiseDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public List<MaterialReqJobwiseModel> GetMaterialReqJobwise(string costCtr, string projectNo)
        {
            var result = new List<MaterialReqJobwiseModel>();
            
            // Clean the input - trim spaces
            costCtr = costCtr?.Trim() ?? string.Empty;
            projectNo = projectNo?.Trim() ?? string.Empty;
            
            Logger.Info($"GetMaterialReqJobwise called with costCtr='{costCtr}', projectNo='{projectNo}'");
            System.Diagnostics.Debug.WriteLine($"=== DEBUG: Querying with costCtr: '{costCtr}', projectNo: '{projectNo}' ===");

            string query = @"
        SELECT A.doc_no, A.doc_pf, A.req_dt, B.res_cd, B.req_units, A.req_cost,
               A.issue_doc_pf, A.issue_doc_no, A.req_source,
               (SELECT (CASE WHEN T1.add_deduct = 'F' THEN T1.trx_qty
                             WHEN T1.add_deduct = 'T' THEN -T1.trx_qty
                             ELSE 0.00 END)
                FROM inpostmt T1, intrhmt T2
                WHERE T1.doc_no = T2.doc_no
                  AND T1.doc_pf = T2.doc_pf
                  AND T1.dept_id = T2.dept_id
                  AND T2.des_dept_id = :costctr
                  AND (T2.issue_to = 1 OR T2.rc_from = 1)
                  AND T2.src_doc_no = A.doc_no
                  AND T2.doc_no = A.issue_doc_no
                  AND (TRIM(T2.is_ref) = TRIM(A.req_source) OR TRIM(T2.rc_ref) = TRIM(A.req_source))
                  AND TRIM(B.res_cd) = TRIM(T1.mat_cd)) AS issued_qty,
               B.issued_val,
               (CASE WHEN A.status = 4 THEN 'Issue Generated'
                     WHEN A.status = 6 THEN 'Issue Posting'
                     WHEN A.status = 7 THEN 'Requisition Confirm '
                     WHEN A.status = 9 THEN 'Posted Cancellation '
                     WHEN A.status = 1 THEN 'Approved '
                     WHEN A.status = 2 THEN 'Approved for Issued Return'
                     WHEN A.status = 3 THEN 'Requested Approved'
                     WHEN A.status = 8 THEN 'Transfer to GL'
                     ELSE NULL END) AS tranStatus,
               (SELECT T3.estimate_qty FROM pcesthmt T1, pcestdmt T3
                WHERE T1.estimate_no = T3.estimate_no
                  AND T1.dept_id = T3.dept_id
                  AND TRIM(T1.project_no) = TRIM(A.req_source)
                  AND T1.dept_id = A.dept_id
                  AND TRIM(T3.res_cd) = TRIM(B.res_cd)
                  AND T3.res_cat = 1) AS est_qty,
               (SELECT T3.commited_qty FROM pcesthmt T1, pcestdmt T3
                WHERE T1.estimate_no = T3.estimate_no
                  AND T1.dept_id = T3.dept_id
                  AND TRIM(T1.project_no) = TRIM(A.req_source)
                  AND T1.dept_id = A.dept_id
                  AND TRIM(T3.res_cd) = TRIM(B.res_cd)
                  AND T3.res_cat = 1) AS com_qty
        FROM mtreqhmt A, mtreqdmt B
        WHERE A.dept_id = B.dept_id
          AND A.doc_pf = B.doc_pf
          AND A.doc_no = B.doc_no
          AND TRIM(A.dept_id) = TRIM(:costctr)
          AND TRIM(A.req_source) = TRIM(:projectno)
        ORDER BY A.doc_pf, A.doc_no, B.res_cd";

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

                    // DEBUG: Check if records exist with TRIM match
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
                            {
                                result.Add(new MaterialReqJobwiseModel
                                {
                                    DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                                    DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                                    ReqDt = reader["req_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["req_dt"]),
                                    ResCd = reader["res_cd"] == DBNull.Value ? null : reader["res_cd"].ToString(),
                                    ReqUnits = reader["req_units"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["req_units"]),
                                    ReqCost = reader["req_cost"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["req_cost"]),
                                    IssueDocPf = reader["issue_doc_pf"] == DBNull.Value ? null : reader["issue_doc_pf"].ToString(),
                                    IssueDocNo = reader["issue_doc_no"] == DBNull.Value ? null : reader["issue_doc_no"].ToString(),
                                    ReqSource = reader["req_source"] == DBNull.Value ? null : reader["req_source"].ToString(),
                                    IssuedQty = reader["issued_qty"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["issued_qty"]),
                                    IssuedVal = reader["issued_val"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["issued_val"]),
                                    TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                                    EstQty = reader["est_qty"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["est_qty"]),
                                    ComQty = reader["com_qty"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["com_qty"])
                                });
                            }
                            Logger.Info($"Main query returned {recordCount} records");
                            System.Diagnostics.Debug.WriteLine($"=== Main Query Result: {recordCount} records ===");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetMaterialReqJobwise: {ex.Message}", ex);
                System.Diagnostics.Debug.WriteLine($"=== ERROR: {ex.Message}\n{ex.StackTrace} ===");
                throw;
            }
            return result;
        }
    }
}