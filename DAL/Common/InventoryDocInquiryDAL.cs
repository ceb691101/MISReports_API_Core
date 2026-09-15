using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class InventoryDocInquiryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<InventoryDocInquiryModel> GetInventoryDocInquiry(string docNo)
        {
            var result = new List<InventoryDocInquiryModel>();

            // Clean the input - remove any extra spaces
            docNo = docNo?.Trim() ?? string.Empty;

            const string query = @"
                SELECT
                    A.doc_no,
                    A.doc_pf,
                    A.trx_dt,
                    A.ent_by,
                    A.modi_by,
                    A.apprv_uid1, 
                    A.Is_Ref,
                    A.Des_dept_id,
                    CASE WHEN A.Issue_to = 1 THEN 'ProvinceJob'
                         WHEN A.Issue_to = 2 THEN 'Cost Center'
                         WHEN A.Issue_to = 3 THEN 'W/HOUSE'
                         WHEN A.Issue_to = 4 THEN 'SUB CON'
                         WHEN A.Issue_to = 5 THEN 'DEPOT-JOB'
                         WHEN A.Issue_to = 6 THEN 'MAINTENANCE'
                         ELSE NULL
                    END AS Issue_to,  
                    A.Rc_Ref,
                    A.src_doc_no,
                    a.src_dept_id,
                    a.ref_1,
                    a.ref_2,
                    a.ref_3,
                    a.ref_4,
                    A.trxn_val,
                    A.remarks,
                    b.yr_ind, 
                    b.mth_ind,
                    b.trx_type,
                    b.mat_cd, 
                    b.trx_qty,
                    b.unit_cost,
                    b.trx_val,
                    b.wrh_cd,
                    b.grade_cd,
                    CASE WHEN A.status = 1 THEN 'New'
                         WHEN A.status = 2 THEN 'Confirmed Record'
                         WHEN A.status = 3 THEN 'Send for 1st. Approval'
                         WHEN A.status = 4 THEN 'Posted. But not Accounted'
                         WHEN A.status = 5 THEN 'Cancelled Record'
                         WHEN A.status = 6 THEN 'GL Posted'
                         WHEN A.status = 7 THEN 'First Approval'
                         ELSE NULL
                    END AS tranStatus
                FROM inpostmt b
                INNER JOIN intrhmt a ON a.doc_no = b.doc_no
                    AND a.doc_pf = b.doc_pf
                    AND a.dept_id = b.dept_id
                WHERE TRIM(a.doc_no) = :payslipParam

                UNION ALL

                SELECT
                    A.doc_no,
                    A.doc_pf,
                    A.req_dt AS trx_dt,
                    A.ent_by,
                    A.modi_by,
                    A.apr_uid_1 AS apprv_uid1,
                    A.req_source AS Is_Ref,
                    A.dept_id AS Des_dept_id,
                    CASE WHEN A.req_source = '1' THEN 'ProvinceJob'
                         WHEN A.req_source = '2' THEN 'Cost Center'
                         WHEN A.req_source = '3' THEN 'W/HOUSE'
                         WHEN A.req_source = '4' THEN 'SUB CON'
                         WHEN A.req_source = '5' THEN 'DEPOT-JOB'
                         WHEN A.req_source = '6' THEN 'MAINTENANCE'
                         ELSE A.req_source
                    END AS Issue_to,  
                    A.req_source AS Rc_Ref,
                    A.issue_doc_no AS src_doc_no,
                    A.issue_dept_id AS src_dept_id,
                    a.ref_1,
                    a.ref_2,
                    a.ref_3,
                    a.ref_4,
                    a.req_cost AS trxn_val,
                    A.remarks,
                    0 AS yr_ind, 
                    0 AS mth_ind,
                    '' AS trx_type,
                    b.res_cd AS mat_cd,
                    b.req_units AS trx_qty,
                    b.unit_price AS unit_cost,
                    b.issued_val AS trx_val,
                    a.wrh_cd,
                    b.grade_cd,
                    CASE WHEN a.status = 4 THEN 'Issue Generated'
                         WHEN a.status = 6 THEN 'Issue Posting'
                         WHEN a.status = 7 THEN 'Requisition Confirm'
                         WHEN a.status = 9 THEN 'Posted Cancellation'
                         WHEN a.status = 1 THEN 'Approved'
                         WHEN a.status = 2 THEN 'Approved for Issued Returns'
                         WHEN a.status = 3 THEN 'Requested Approved'
                         WHEN a.status = 8 THEN 'Transfer to GL'
                         ELSE NULL 
                    END AS tranStatus
                FROM mtreqhmt a
                INNER JOIN mtreqdmt b ON a.dept_id = b.dept_id
                    AND a.doc_pf = b.doc_pf
                    AND a.doc_no = b.doc_no
                WHERE TRIM(a.doc_no) = :payslipParam";

            try
            {
                using (var conn = new OracleConnection(_connectionString))
                using (var cmd = new OracleCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.BindByName = true;

                    // Add parameter
                    cmd.Parameters.Add("payslipParam", OracleDbType.Varchar2).Value = docNo;

                    // Debug logging
                    System.Diagnostics.Debug.WriteLine($"=== DEBUG: Querying with docNo: '{docNo}' ===");
                    System.Diagnostics.Debug.WriteLine($"=== Parameter Value: '{cmd.Parameters[0].Value}' ===");

                    conn.Open();

                    // DEBUG: Check if document exists with exact match
                    using (var checkCmd = new OracleCommand(
                        "SELECT COUNT(*) FROM intrhmt WHERE TRIM(doc_no) = :docNo", conn))
                    {
                        checkCmd.Parameters.Add("docNo", OracleDbType.Varchar2).Value = docNo;
                        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        System.Diagnostics.Debug.WriteLine($"=== Records in intrhmt (exact match): {count} ===");

                        // If zero, check with LIKE to see similar records
                        if (count == 0)
                        {
                            using (var likeCmd = new OracleCommand(
                                "SELECT doc_no FROM intrhmt WHERE ROWNUM <= 5", conn))
                            {
                                using (var reader = likeCmd.ExecuteReader())
                                {
                                    System.Diagnostics.Debug.WriteLine("=== Sample doc_no values in database: ===");
                                    while (reader.Read())
                                    {
                                        System.Diagnostics.Debug.WriteLine($"  - '{reader[0].ToString()}'");
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
                            var model = new InventoryDocInquiryModel
                            {
                                DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                                DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                                TrxDt = reader["trx_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["trx_dt"]),
                                EntBy = reader["ent_by"] == DBNull.Value ? null : reader["ent_by"].ToString(),
                                ModiBy = reader["modi_by"] == DBNull.Value ? null : reader["modi_by"].ToString(),
                                ApprvUid1 = reader["apprv_uid1"] == DBNull.Value ? null : reader["apprv_uid1"].ToString(),
                                IsRef = reader["is_ref"] == DBNull.Value ? null : reader["is_ref"].ToString(),
                                DesDeptId = reader["des_dept_id"] == DBNull.Value ? null : reader["des_dept_id"].ToString(),
                                IssueTo = reader["issue_to"] == DBNull.Value ? null : reader["issue_to"].ToString(),
                                RcRef = reader["rc_ref"] == DBNull.Value ? null : reader["rc_ref"].ToString(),
                                SrcDocNo = reader["src_doc_no"] == DBNull.Value ? null : reader["src_doc_no"].ToString(),
                                SrcDeptId = reader["src_dept_id"] == DBNull.Value ? null : reader["src_dept_id"].ToString(),
                                Ref1 = reader["ref_1"] == DBNull.Value ? null : reader["ref_1"].ToString(),
                                Ref2 = reader["ref_2"] == DBNull.Value ? null : reader["ref_2"].ToString(),
                                Ref3 = reader["ref_3"] == DBNull.Value ? null : reader["ref_3"].ToString(),
                                Ref4 = reader["ref_4"] == DBNull.Value ? null : reader["ref_4"].ToString(),
                                TrxnVal = reader["trxn_val"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["trxn_val"]),
                                Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                                YrInd = reader["yr_ind"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["yr_ind"]),
                                MthInd = reader["mth_ind"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["mth_ind"]),
                                TrxType = reader["trx_type"] == DBNull.Value ? null : reader["trx_type"].ToString(),
                                MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                                TrxQty = reader["trx_qty"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["trx_qty"]),
                                UnitCost = reader["unit_cost"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["unit_cost"]),
                                TrxVal = reader["trx_val"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["trx_val"]),
                                WrhCd = reader["wrh_cd"] == DBNull.Value ? null : reader["wrh_cd"].ToString(),
                                GradeCd = reader["grade_cd"] == DBNull.Value ? null : reader["grade_cd"].ToString(),
                                TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString()
                            };
                            result.Add(model);
                        }
                        System.Diagnostics.Debug.WriteLine($"=== Total records returned: {recordCount} ===");
                    }
                }
            }
            catch (OracleException ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== ORACLE ERROR: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"Error Code: {ex.ErrorCode}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== GENERAL ERROR: {ex.Message} ===");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }

            return result;
        }
    }
}