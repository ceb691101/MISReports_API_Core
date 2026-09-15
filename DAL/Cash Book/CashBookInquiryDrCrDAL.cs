using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class CashbookInquiryDrCrDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CashbookInquiryDrCrModel> GetCashbookInquiryDrCr(string fromDate, string toDate, string costCtr)
        {
            var result = new List<CashbookInquiryDrCrModel>();
            const string query = @"
                SELECT A.doc_no, A.doc_dt, B.exp_cd, B.sub_ac, B.dr_amt, B.cr_amt, A.non_taxabl,
                       (CASE WHEN A.status = 1 THEN 'New'
                             WHEN A.status = 2 THEN 'Send for Approval'
                             WHEN A.status = 3 THEN 'Approved'
                             WHEN A.status = 4 THEN 'Transfer to GL'
                             WHEN A.status = 6 THEN 'To be cancelled'
                             WHEN A.status = 5 THEN 'Cancelled  Record'
                             WHEN A.status = 7 THEN 'Payment Plan generated '
                             WHEN A.status = 8 THEN 'PP'
                             ELSE NULL END) AS transtatus,
                       A.payee, A.remarks,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM cbpmthmt A, cbpmtett B
                WHERE A.dept_id = :costctr
                  AND A.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND A.doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
                  AND A.doc_no = B.doc_no
                  AND A.doc_pf = B.doc_pf
                  AND A.dept_id = B.dept_id
                  AND B.exp_cd <> 'L9001'
                ORDER BY A.doc_pf, A.doc_no, A.doc_dt, B.exp_cd";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CashbookInquiryDrCrModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            ExpCd = reader["exp_cd"] == DBNull.Value ? null : reader["exp_cd"].ToString(),
                            SubAc = reader["sub_ac"] == DBNull.Value ? null : reader["sub_ac"].ToString(),
                            DrAmt = reader["dr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["dr_amt"]),
                            CrAmt = reader["cr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["cr_amt"]),
                            NonTaxabl = reader["non_taxabl"] == DBNull.Value ? null : reader["non_taxabl"].ToString(),
                            TranStatus = reader["transtatus"] == DBNull.Value ? null : reader["transtatus"].ToString(),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}