using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class DocInquiryGlDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<DocInquiryGlModel> GetDocInquiryGl(string fromDate, string toDate, string costCtr)
        {
            var result = new List<DocInquiryGlModel>();

            string query = @"
        SELECT A.doc_pf, A.doc_no, A.doc_dt, B.gl_cd, B.dr_amt, B.cr_amt, A.trx_val,
               A.remarks, A.ref_1, A.ref_2, A.trf_dept,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM glvochmt A, glvocdmt B
        WHERE A.dept_id = :costctr
          AND A.dept_id = B.dept_id
          AND A.doc_no = B.doc_no
          AND A.doc_pf = B.doc_pf
          AND A.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND A.doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        ORDER BY A.doc_pf, A.doc_no, A.doc_dt, B.gl_cd";

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
                        result.Add(new DocInquiryGlModel
                        {
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            GlCd = reader["gl_cd"] == DBNull.Value ? null : reader["gl_cd"].ToString(),
                            DrAmt = reader["dr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["dr_amt"]),
                            CrAmt = reader["cr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["cr_amt"]),
                            TrxVal = reader["trx_val"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["trx_val"]),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            Ref1 = reader["ref_1"] == DBNull.Value ? null : reader["ref_1"].ToString(),
                            Ref2 = reader["ref_2"] == DBNull.Value ? null : reader["ref_2"].ToString(),
                            TrfDept = reader["trf_dept"] == DBNull.Value ? null : reader["trf_dept"].ToString(),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}