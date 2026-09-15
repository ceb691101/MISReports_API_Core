using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class GlInquiryByDocDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<GlInquiryByDocModel> GetGlInquiryByDoc(string paySlip)
        {
            var result = new List<GlInquiryByDocModel>();

            // Clean the input - remove any extra spaces (same as working API)
            paySlip = paySlip?.Trim() ?? string.Empty;

            string query = @"
        SELECT A.doc_no, A.dept_id, A.doc_pf, A.doc_dt, A.ent_by, A.modi_by, A.appr_by,
               B.dr_amt, B.cr_amt, B.gl_cd, B.sub_ac, A.remarks, A.trx_val,
               (CASE WHEN A.status = 1 THEN 'New'
                     WHEN A.status = 2 THEN 'Confirmed'
                     WHEN A.status = 6 THEN 'GL Posted'
                     ELSE NULL END) AS transtatus
        FROM glvochmt A, glvocdmt B
        WHERE A.doc_no = B.doc_no
          AND A.batch_id = B.batch_id
          AND A.doc_pf = B.doc_pf
          AND A.dept_id = B.dept_id
          AND TRIM(A.doc_no) = :payslip  -- Added TRIM here
        UNION ALL
        SELECT A.doc_no, A.dept_id, A.doc_pf, A.doc_dt, A.ent_by, A.modi_by, A.appr_by,
               B.dr_amt, B.cr_amt, B.gl_cd, B.sub_ac, A.remarks, A.trx_val,
               (CASE WHEN A.status = 1 THEN 'New'
                     WHEN A.status = 2 THEN 'Confirmed'
                     WHEN A.status = 3 THEN 'Edit Batch'
                     WHEN A.status = 4 THEN 'Edited Batch'
                     WHEN A.status = 5 THEN 'Generated'
                     WHEN A.status = 6 THEN 'Printed'
                     ELSE NULL END) AS transtatus
        FROM glvochtt A, glvocdtt B
        WHERE NOT (A.status = 0)
          AND A.doc_no = B.doc_no
          AND A.batch_id = B.batch_id
          AND A.doc_pf = B.doc_pf
          AND A.dept_id = B.dept_id
          AND TRIM(A.doc_no) = :payslip  -- Added TRIM here
        ORDER BY 2, 1, 3, 4";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                // Parameter name should match exactly what's in the query
                cmd.Parameters.Add(new OracleParameter("payslip", OracleDbType.Varchar2) { Value = paySlip });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new GlInquiryByDocModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            EntBy = reader["ent_by"] == DBNull.Value ? null : reader["ent_by"].ToString(),
                            ModiBy = reader["modi_by"] == DBNull.Value ? null : reader["modi_by"].ToString(),
                            ApprBy = reader["appr_by"] == DBNull.Value ? null : reader["appr_by"].ToString(),
                            DrAmt = reader["dr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["dr_amt"]),
                            CrAmt = reader["cr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["cr_amt"]),
                            GlCd = reader["gl_cd"] == DBNull.Value ? null : reader["gl_cd"].ToString(),
                            SubAc = reader["sub_ac"] == DBNull.Value ? null : reader["sub_ac"].ToString(),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            TrxVal = reader["trx_val"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["trx_val"]),
                            TranStatus = reader["transtatus"] == DBNull.Value ? null : reader["transtatus"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}