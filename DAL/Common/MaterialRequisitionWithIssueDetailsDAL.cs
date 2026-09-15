using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class MaterialRequisitionWithIssueDetailsDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<MaterialRequisitionWithIssueDetailsModel> GetMaterialRequisitionWithIssueDetails(
            string fromDate, string toDate, string costCtr, string matCode)
        {
            var result = new List<MaterialRequisitionWithIssueDetailsModel>();

            const string query = @"
                SELECT 'Material Requistion' AS category,
                       A.doc_no,
                       A.req_dt AS trx_dt,
                       B.res_cd AS mat_cd,
                       B.req_units,
                       A.issue_doc_no,
                       A.req_source,
                       A.ref_1,
                       A.apr_uid_1,
                       A.qty_apr_uid AS apr_uid_2,
                       A.apr_dt_1,
                       A.qty_apr_dt AS apr_dt_2,
                       (SELECT T1.trx_dt
                        FROM inpostmt T1, intrhmt T2
                        WHERE T1.doc_no = T2.doc_no
                          AND T1.doc_pf = T2.doc_pf
                          AND T1.dept_id = T2.dept_id
                          AND T2.des_dept_id = :costctr
                          AND (T2.issue_to = 1)
                          AND T2.src_doc_no = A.doc_no
                          AND T2.doc_no = A.issue_doc_no
                          AND TRIM(B.res_cd) = TRIM(T1.mat_cd)) AS post_dt,
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
                          AND TRIM(B.res_cd) = TRIM(T1.mat_cd)) AS issued_return_qty,
                       (CASE
                            WHEN A.status = 4 THEN 'Issue Generated'
                            WHEN A.status = 6 THEN 'Issue Posting'
                            WHEN A.status = 7 THEN 'Requisition Confirm '
                            WHEN A.status = 9 THEN 'Posted Cancellation '
                            WHEN A.status = 1 THEN 'Approved '
                            WHEN A.status = 2 THEN 'Approved for Issued Returns '
                            WHEN A.status = 3 THEN 'Requested Approved'
                            WHEN A.status = 8 THEN 'Transfer to GL'
                            ELSE NULL
                        END) AS transtatus,
                       (SELECT T3.estimate_qty
                        FROM pcesthmt T1, pcestdmt T3
                        WHERE T1.estimate_no = T3.estimate_no
                          AND T1.dept_id = T3.dept_id
                          AND TRIM(T1.project_no) = TRIM(A.req_source)
                          AND T1.dept_id = A.dept_id
                          AND TRIM(T3.res_cd) = TRIM(B.res_cd)
                          AND T3.res_cat = 1) AS est_qty,
                       (SELECT T3.commited_qty
                        FROM pcesthmt T1, pcestdmt T3
                        WHERE T1.estimate_no = T3.estimate_no
                          AND T1.dept_id = T3.dept_id
                          AND TRIM(T1.project_no) = TRIM(A.req_source)
                          AND T1.dept_id = A.dept_id
                          AND TRIM(T3.res_cd) = TRIM(B.res_cd)
                          AND T3.res_cat = 1) AS com_qty,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM mtreqhmt A, mtreqdmt B
                WHERE A.dept_id = B.dept_id
                  AND A.doc_pf = B.doc_pf
                  AND A.doc_no = B.doc_no
                  AND A.dept_id = :costctr
                  AND A.req_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                  AND A.req_dt <= TO_DATE(:todate, 'yyyy/mm/dd')
                  AND A.req_from = 3
                  AND TRIM(B.res_cd) LIKE :matcode || '%'
                GROUP BY B.res_cd, A.doc_no, A.req_dt, B.req_units, A.issue_doc_no, A.req_source,
                         A.dept_id, A.status, A.ref_1, A.apr_uid_1, A.qty_apr_uid, A.apr_dt_1, A.qty_apr_dt

                UNION ALL

                SELECT 'Cost Center' AS category,
                       A.doc_no,
                       A.req_dt AS trx_dt,
                       B.res_cd AS mat_cd,
                       B.req_units,
                       A.issue_doc_no,
                       '' AS req_source,
                       '' AS ref_1,
                       A.apr_uid_1,
                       A.qty_apr_uid AS apr_uid_2,
                       A.apr_dt_1,
                       A.qty_apr_dt AS apr_dt_2,
                       (SELECT T1.trx_dt
                        FROM inpostmt T1, intrhmt T2
                        WHERE T1.doc_no = T2.doc_no
                          AND T1.doc_pf = T2.doc_pf
                          AND T1.dept_id = T2.dept_id
                          AND T2.des_dept_id = :costctr
                          AND (T2.issue_to = 2)
                          AND T2.src_doc_no = A.doc_no
                          AND T2.doc_no = A.issue_doc_no
                          AND TRIM(B.res_cd) = TRIM(T1.mat_cd)) AS post_dt,
                       (SELECT (CASE WHEN T1.add_deduct = 'F' THEN T1.trx_qty
                                     WHEN T1.add_deduct = 'T' THEN -T1.trx_qty
                                     ELSE 0.00 END)
                        FROM inpostmt T1, intrhmt T2
                        WHERE T1.doc_no = T2.doc_no
                          AND T1.doc_pf = T2.doc_pf
                          AND T1.dept_id = T2.dept_id
                          AND T2.des_dept_id = :costctr
                          AND (T2.issue_to = 2)
                          AND T2.src_doc_no = A.doc_no
                          AND T2.doc_no = A.issue_doc_no
                          AND TRIM(B.res_cd) = TRIM(T1.mat_cd)) AS issued_return_qty,
                       (CASE
                            WHEN A.status = 4 THEN 'Issue Generated'
                            WHEN A.status = 6 THEN 'Issue Posting'
                            WHEN A.status = 7 THEN 'Requisition Confirm '
                            WHEN A.status = 9 THEN 'Posted Cancellation '
                            WHEN A.status = 1 THEN 'Approved '
                            WHEN A.status = 2 THEN 'Approved for Issued Returns '
                            WHEN A.status = 3 THEN 'Requested Approved'
                            WHEN A.status = 8 THEN 'Transfer to GL'
                            ELSE NULL
                        END) AS transtatus,
                       NULL AS est_qty,
                       NULL AS com_qty,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM mtreqhmt A, mtreqdmt B
                WHERE A.dept_id = B.dept_id
                  AND A.doc_pf = B.doc_pf
                  AND A.doc_no = B.doc_no
                  AND A.dept_id = :costctr
                  AND A.req_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                  AND A.req_dt <= TO_DATE(:todate, 'yyyy/mm/dd')
                  AND A.req_from = 1
                  AND TRIM(B.res_cd) LIKE :matcode || '%'
                GROUP BY B.res_cd, A.doc_no, A.req_dt, B.req_units, A.issue_doc_no,
                         A.dept_id, A.status, A.apr_uid_1, A.qty_apr_uid, A.apr_dt_1, A.qty_apr_dt

                ORDER BY 1, 4, 7, 2";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required - :costctr / :matcode / :fromdate / :todate are each reused across both blocks
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });
                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2) { Value = matCode });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new MaterialRequisitionWithIssueDetailsModel
                        {
                            Category = reader["category"] == DBNull.Value ? null : reader["category"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            TrxDt = reader["trx_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["trx_dt"]),
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            ReqUnits = GetSafeDecimal(reader, "req_units"),
                            IssueDocNo = reader["issue_doc_no"] == DBNull.Value ? null : reader["issue_doc_no"].ToString(),
                            ReqSource = reader["req_source"] == DBNull.Value ? null : reader["req_source"].ToString(),
                            Ref1 = reader["ref_1"] == DBNull.Value ? null : reader["ref_1"].ToString(),
                            AprUid1 = reader["apr_uid_1"] == DBNull.Value ? null : reader["apr_uid_1"].ToString(),
                            AprUid2 = reader["apr_uid_2"] == DBNull.Value ? null : reader["apr_uid_2"].ToString(),
                            AprDt1 = reader["apr_dt_1"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["apr_dt_1"]),
                            AprDt2 = reader["apr_dt_2"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["apr_dt_2"]),
                            PostDt = reader["post_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["post_dt"]),
                            IssuedReturnQty = GetSafeDecimal(reader, "issued_return_qty"),
                            TranStatus = reader["transtatus"] == DBNull.Value ? null : reader["transtatus"].ToString(),
                            EstQty = GetSafeDecimal(reader, "est_qty"),
                            ComQty = GetSafeDecimal(reader, "com_qty"),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Safely reads a NUMBER column as a nullable decimal.
        /// Oracle's NUMBER type can carry more precision than .NET's decimal can hold,
        /// which makes a plain Convert.ToDecimal(reader[...]) throw OverflowException
        /// on some rows. Reading via OracleDecimal and clamping precision first avoids that.
        /// </summary>
        private static decimal? GetSafeDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;

            OracleDecimal oraVal = reader.GetOracleDecimal(ordinal);
            oraVal = OracleDecimal.SetPrecision(oraVal, 28);

            return oraVal.Value;
        }
    }
}