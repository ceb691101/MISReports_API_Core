using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using NLog;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class ProvinceCashBookInquiryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public List<ProvinceCashBookInquiryModel> GetProvinceCashBookInquiry(string curDate, string compId)
        {
            var result = new List<ProvinceCashBookInquiryModel>();
            
            // Clean the input - trim spaces
            compId = compId?.Trim() ?? string.Empty;
            
            Logger.Info($"GetProvinceCashBookInquiry called with curDate='{curDate}', compId='{compId}'");
            System.Diagnostics.Debug.WriteLine($"=== DEBUG: Querying with curDate: '{curDate}', compId: '{compId}' ===");

            string query = @"
        SELECT DISTINCT 'Cash Book- A' AS Category, C.dept_id, C.doc_dt, C.non_taxabl, C.doc_no,
               (CASE WHEN C.status IN (1, 2) THEN '' ELSE C.apprv_uid1 END) AS apprv_uid1,
               (CASE WHEN C.status IN (1, 2) THEN '' ELSE TO_CHAR(C.appr_dt,'yyyy/mm/dd') END) AS appr_dt1,
               (CASE WHEN C.status = 1 THEN 'New'
                     WHEN C.status = 2 THEN 'Send for Approval'
                     WHEN C.status = 3 THEN 'Approved'
                     WHEN C.status = 4 THEN 'Transfer to GL'
                     WHEN C.status = 6 THEN 'To be cancelled'
                     WHEN C.status = 5 THEN 'Cancelled  Record'
                     WHEN C.status = 7 THEN 'Payment Plan generated '
                     WHEN C.status = 8 THEN 'GL Posted'
                     ELSE NULL END) AS tranStatus,
               C.payee, TO_CHAR(A.chq_dt,'yyyy/mm/dd') AS chq_dt, A.chq_no, A.chq_run AS pymt_docno,
               (SELECT CASE WHEN B.status = 1 THEN 'Approved  Payment Plan'
                            WHEN B.status = 3 THEN 'Cheque printed'
                            WHEN B.status = 5 THEN 'Transfer to GL'
                            WHEN B.status = 7 THEN 'Cheque assignment Report'
                            WHEN B.status = 8 THEN 'Confirmation'
                            ELSE NULL END
                FROM cbchqhmt B WHERE TRIM(A.chq_run) = TRIM(B.chq_run)
                UNION ALL
                SELECT CASE WHEN B.status = 1 THEN 'Create Payment Plan'
                            WHEN B.status = 3 THEN 'Print PP Final report'
                            WHEN B.status = 4 THEN 'Edit Payment  Plan'
                            WHEN B.status = 6 THEN 'Send for second approval'
                            WHEN B.status = 5 THEN 'Send  PP for Approval'
                            ELSE NULL END
                FROM cbchqhtt B WHERE TRIM(A.chq_run) = TRIM(B.chq_run) AND NOT (B.status = 0)) AS PP_Status,
               (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS comp_NM
        FROM (cbpmthmt C LEFT OUTER JOIN cbchqdmt A ON
               A.pymt_docpf = C.doc_pf AND
               A.pymt_docno = C.doc_no AND
               TRIM(A.chq_run) = TRIM(C.chq_run) AND
               TRIM(A.chq_no) = TRIM(C.chq_no))
        WHERE doc_dt >= TO_DATE(:curdate,'yyyy/mm/dd')
          AND doc_dt <= (TO_DATE(:curdate,'yyyy/mm/dd') + 7)
          AND C.dept_id IN (
              SELECT dept_id FROM gldeptm
              WHERE comp_id IN (
                  SELECT comp_id FROM glcompm
                  WHERE TRIM(parent_id) = TRIM(:compid) OR TRIM(comp_id) = TRIM(:compid)
              )
          )
        UNION ALL
        SELECT DISTINCT 'Cash Book-T' AS Category, dept_id, doc_dt, non_taxabl, doc_no,
               (CASE WHEN status IN (1) THEN ''
                     WHEN status IN (2) THEN '** To be Approved by  ' || apprv_uid1
                     ELSE apprv_uid1 END) AS apprv_uid1,
               (CASE WHEN status IN (1, 2) THEN '' ELSE TO_CHAR(appr_dt,'yyyy/mm/dd') END) AS appr_dt1,
               (CASE WHEN status = 1 THEN 'New Record'
                     WHEN status = 2 THEN 'Send for 1st. Approval'
                     WHEN status = 3 THEN 'Approved'
                     WHEN status = 4 THEN 'Rejected'
                     WHEN status = 6 THEN 'Send for second approval'
                     WHEN status = 7 THEN 'Approved Once'
                     WHEN status = 8 THEN 'Printed '
                     WHEN status = 9 THEN 'Cancelled'
                     ELSE NULL END) AS tranStatus,
               payee, '' AS chq_dt, '' AS chq_no, '' AS pymt_docno, '' AS PP_Status,
               (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS comp_NM
        FROM cbpmthtt
        WHERE NOT (status = 0)
          AND doc_dt >= TO_DATE(:curdate,'yyyy/mm/dd')
          AND doc_dt <= (TO_DATE(:curdate,'yyyy/mm/dd') + 7)
          AND dept_id IN (
              SELECT dept_id FROM gldeptm
              WHERE comp_id IN (
                  SELECT comp_id FROM glcompm
                  WHERE TRIM(parent_id) = TRIM(:compid) OR TRIM(comp_id) = TRIM(:compid)
              )
          )
        ORDER BY 2, 1, 4";

            try
            {
                using (var conn = new OracleConnection(_connectionString))
                using (var cmd = new OracleCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compId });
                    cmd.Parameters.Add(new OracleParameter("curdate", OracleDbType.Varchar2) { Value = curDate });

                    System.Diagnostics.Debug.WriteLine($"=== Parameter compId: '{cmd.Parameters[0].Value}' ===");
                    System.Diagnostics.Debug.WriteLine($"=== Parameter curDate: '{cmd.Parameters[1].Value}' ===");

                    conn.Open();

                    // DEBUG: Check if comp_id exists in glcompm
                    using (var checkCmd = new OracleCommand(
                        "SELECT COUNT(*) FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid) OR TRIM(parent_id) = TRIM(:compid)", conn))
                    {
                        checkCmd.Parameters.Add("compid", OracleDbType.Varchar2).Value = compId;
                        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        Logger.Info($"Companies found with TRIM match: {count}");
                        System.Diagnostics.Debug.WriteLine($"=== Records in glcompm (TRIM match): {count} ===");

                        // If zero, check what values exist
                        if (count == 0)
                        {
                            Logger.Info($"No comp_id found. Checking existing comp_id values...");
                            
                            using (var sampleCmd = new OracleCommand(
                                "SELECT DISTINCT TRIM(comp_id) FROM glcompm WHERE ROWNUM <= 10", conn))
                            {
                                using (var reader = sampleCmd.ExecuteReader())
                                {
                                    System.Diagnostics.Debug.WriteLine($"=== Sample comp_id values in database: ===");
                                    Logger.Info($"Sample comp_id values in database:");
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
                                        System.Diagnostics.Debug.WriteLine($"  (No companies found)");
                                        Logger.Info($"  (No companies found)");
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
                            result.Add(new ProvinceCashBookInquiryModel
                        {
                            Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            NonTaxabl = reader["non_taxabl"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["non_taxabl"]),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            ApprvUid1 = reader["apprv_uid1"] == DBNull.Value ? null : reader["apprv_uid1"].ToString(),
                            ApprDt1 = reader["appr_dt1"] == DBNull.Value ? null : reader["appr_dt1"].ToString(),
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            ChqDt = reader["chq_dt"] == DBNull.Value ? null : reader["chq_dt"].ToString(),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            PymtDocNo = reader["pymt_docno"] == DBNull.Value ? null : reader["pymt_docno"].ToString(),
                            PpStatus = reader["PP_Status"] == DBNull.Value ? null : reader["PP_Status"].ToString(),
                            BranchName = reader["comp_NM"] == DBNull.Value ? null : reader["comp_NM"].ToString()
                        });
                        }
                        Logger.Info($"Main query returned {recordCount} records");
                        System.Diagnostics.Debug.WriteLine($"=== Main Query Result: {recordCount} records ===");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in GetProvinceCashBookInquiry: {ex.Message}", ex);
                System.Diagnostics.Debug.WriteLine($"=== ERROR: {ex.Message}\n{ex.StackTrace} ===");
                throw;
            }
            return result;
        }
    }
}