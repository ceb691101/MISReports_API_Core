using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class ChequeDetailsExpDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ChequeDetailsExpModel> GetChequeDetailsExp(string costCtr, string acctCode, string fromDate, string toDate)
        {
            var result = new List<ChequeDetailsExpModel>();

            const string query = @"
                SELECT A.chq_dt,
                       A.chq_no,
                       A.pymt_docno,
                       D.exp_cd,
                       (CASE WHEN D.dr_amt > 0 THEN D.dr_amt
                             WHEN D.cr_amt > 0 THEN (-1) * D.cr_amt
                             ELSE NULL END) AS dr_amt,
                       B.chq_amt,
                       A.payee,
                       SUBSTR(C.remarks, 1, 150) AS remarks,
                       C.ref_1,
                       C.ref_2,
                       C.ref_3,
                       C.ref_4,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM cbchqdmt A, cbchqrgh B, cbpmthmt C, cbpmtemt D
                WHERE A.pymt_docno = C.doc_no
                  AND A.pymt_docno = D.doc_no
                  AND A.pymt_docpf = C.doc_pf
                  AND TRIM(A.chq_run) = TRIM(C.chq_run)
                  AND TRIM(A.chq_run) = TRIM(B.chq_run)
                  AND TRIM(A.chq_no) = TRIM(B.chq_no)
                  AND TRIM(A.chq_no) = TRIM(C.chq_no)
                  AND C.doc_pf = D.doc_pf
                  AND C.dept_id = D.dept_id
                  AND A.dept_id = :costctr
                  AND D.exp_cd LIKE '%' || :acctcode || '%'
                  AND (A.chq_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                       AND A.chq_dt <= TO_DATE(:todate, 'yyyy/mm/dd'))
                  AND (D.dr_amt > 0 OR D.cr_amt > 0)
                ORDER BY D.exp_cd, A.chq_no, A.grp_id";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("acctcode", OracleDbType.Varchar2) { Value = acctCode });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ChequeDetailsExpModel
                        {
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            PymtDocNo = reader["pymt_docno"] == DBNull.Value ? null : reader["pymt_docno"].ToString(),
                            ExpCd = reader["exp_cd"] == DBNull.Value ? null : reader["exp_cd"].ToString(),
                            DrAmt = reader["dr_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["dr_amt"]),
                            ChqAmt = reader["chq_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["chq_amt"]),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            Ref1 = reader["ref_1"] == DBNull.Value ? null : reader["ref_1"].ToString(),
                            Ref2 = reader["ref_2"] == DBNull.Value ? null : reader["ref_2"].ToString(),
                            Ref3 = reader["ref_3"] == DBNull.Value ? null : reader["ref_3"].ToString(),
                            Ref4 = reader["ref_4"] == DBNull.Value ? null : reader["ref_4"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}