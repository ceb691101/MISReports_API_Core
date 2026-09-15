using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class InquiryCashBookUnpostedCancelDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<InquiryCashBookUnpostedCancelModel> GetInquiryCashBookUnpostedCancel(string fromDate, string toDate, string costCtr)
        {
            var result = new List<InquiryCashBookUnpostedCancelModel>();

            string query = @"
        SELECT DISTINCT
               A.doc_no, A.doc_dt, A.non_taxabl, A.ent_by, A.apprv_uid1,
               (CASE WHEN A.status = 1 THEN 'New Record'
                     WHEN A.status = 2 THEN 'Send for 1st. Approval'
                     WHEN A.status = 3 THEN 'Approved'
                     WHEN A.status = 4 THEN 'Rejected'
                     WHEN A.status = 6 THEN 'Send for second approval'
                     WHEN A.status = 7 THEN 'Approved Once'
                     WHEN A.status = 8 THEN 'Printed '
                     WHEN A.status = 9 THEN 'Cancelled'
                     ELSE NULL END) AS tranStatus,
               B.sent_uid AS rej_by, A.rejc_dt, B.rct_uid AS Cancelled_User, B.sent_dt AS cancel_dt,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM cbpmthtt A, cbmail B
        WHERE (A.status = 9)
          AND A.dept_id = :costctr
          AND B.doc_no = A.doc_no
          AND B.doc_pf = A.doc_pf
          AND B.dept_id = A.dept_id
          AND A.doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND A.doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        ORDER BY 1, 4";

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
                        result.Add(new InquiryCashBookUnpostedCancelModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            NonTaxabl = reader["non_taxabl"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["non_taxabl"]),
                            EntBy = reader["ent_by"] == DBNull.Value ? null : reader["ent_by"].ToString(),
                            ApprvUid1 = reader["apprv_uid1"] == DBNull.Value ? null : reader["apprv_uid1"].ToString(),
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                            RejBy = reader["rej_by"] == DBNull.Value ? null : reader["rej_by"].ToString(),
                            RejcDt = reader["rejc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["rejc_dt"]),
                            CancelledUser = reader["Cancelled_User"] == DBNull.Value ? null : reader["Cancelled_User"].ToString(),
                            CancelDt = reader["cancel_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["cancel_dt"]),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}