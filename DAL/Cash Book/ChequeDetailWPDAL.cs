using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models;

namespace MISReports_Api.DAL
{
    public class ChequeDetailWPDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ChequeDetailWPModel> GetChequeDetailWP(string fromDate, string toDate, string costCtr)
        {
            var result = new List<ChequeDetailWPModel>();

            const string query = @"
                SELECT A.chq_dt,
                       A.chq_no,
                       A.pymt_docno,
                       D.exp_cd,
                       D.dr_amt AS dr_amt,
                       B.chq_amt,
                       A.payee,
                       SUBSTR(C.remarks, 1, 150) AS remarks,
                       C.address,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM cbchqdmt A, cbchqrgh B, cbpmthmt C, cbchqhmt T2, cbpmtemt D
                WHERE A.pymt_docno = C.doc_no
                  AND A.pymt_docno = D.doc_no
                  AND A.pymt_docpf = C.doc_pf
                  AND TRIM(A.chq_run) = TRIM(C.chq_run)
                  AND TRIM(A.chq_run) = TRIM(B.chq_run)
                  AND TRIM(A.chq_no) = TRIM(B.chq_no)
                  AND TRIM(A.chq_no) = TRIM(C.chq_no)
                  AND T2.status NOT IN (8)
                  AND B.status NOT IN (3)
                  AND A.dept_id = T2.dept_id
                  AND A.chq_run = T2.chq_run
                  AND B.chq_run = T2.chq_run
                  AND C.doc_pf = D.doc_pf
                  AND C.dept_id = D.dept_id
                  AND A.dept_id = :costctr
                  AND (A.chq_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd') AND A.chq_dt <= TO_DATE(:todate, 'yyyy/mm/dd'))
                  AND D.dr_amt > 0
                ORDER BY A.chq_no, A.chq_dt, A.pymt_docno";

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
                        result.Add(new ChequeDetailWPModel
                        {
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            PymtDocNo = reader["pymt_docno"] == DBNull.Value ? null : reader["pymt_docno"].ToString(),
                            ExpCd = reader["exp_cd"] == DBNull.Value ? null : reader["exp_cd"].ToString(),
                            DrAmt = reader["dr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["dr_amt"]),
                            ChqAmt = reader["chq_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["chq_amt"]),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            Address = reader["address"] == DBNull.Value ? null : reader["address"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}
