using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class TenderDocInquiryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<TenderDocInquiryModel> GetTenderDocInquiry(string fromDate, string toDate, string costCtr, string refNo)
        {
            var result = new List<TenderDocInquiryModel>();

            string query = @"
        SELECT B.doc_no, B.payee, B.non_taxabl, B.remarks, B.chq_no,
               (SELECT DISTINCT A.chq_dt FROM cbchqrgh A WHERE A.chq_no = B.chq_no AND A.chq_run = B.chq_run) AS chq_dt,
               (SELECT DISTINCT A.chq_amt FROM cbchqrgh A WHERE A.chq_no = B.chq_no AND A.chq_run = B.chq_run) AS chq_amt,
               B.ref_4,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM cbpmthmt B
        WHERE TRIM(B.ref_4) LIKE :refno || '%'
          AND B.dept_id = :costctr
          AND B.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND B.doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
          AND B.ref_4 IS NOT NULL
        ORDER BY 1, 2";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });
                cmd.Parameters.Add(new OracleParameter("refno", OracleDbType.Varchar2) { Value = refNo ?? "" });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TenderDocInquiryModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            NonTaxabl = reader["non_taxabl"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["non_taxabl"]),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            ChqAmt = reader["chq_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["chq_amt"]),
                            Ref4 = reader["ref_4"] == DBNull.Value ? null : reader["ref_4"].ToString(),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}