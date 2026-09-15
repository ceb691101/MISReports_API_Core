using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class InquiryGeneralLedgerDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<InquiryGeneralLedgerModel> GetInquiryGeneralLedger(string fromDate, string toDate, string costCtr)
        {
            var result = new List<InquiryGeneralLedgerModel>();

            string query = @"
        SELECT 'General  Ledger-After Posting' AS Category, doc_no, doc_pf, doc_dt, ent_by, modi_by, appr_by,
               (CASE WHEN status = 1 THEN 'New'
                     WHEN status = 2 THEN 'Confirmed'
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM glvochmt
        WHERE NOT (status = 6)
          AND dept_id = :costctr
          AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        UNION ALL
        SELECT 'General  Ledger- Temporary Transcations' AS Category, doc_no, doc_pf, doc_dt, ent_by, modi_by, appr_by,
               (CASE WHEN status = 1 THEN 'New'
                     WHEN status = 2 THEN 'Confirmed'
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM glvochtt
        WHERE NOT (status = 0)
          AND dept_id = :costctr
          AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        ORDER BY 1, 8, doc_pf, doc_no, doc_dt";

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
                        result.Add(new InquiryGeneralLedgerModel
                        {
                            Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            EntBy = reader["ent_by"] == DBNull.Value ? null : reader["ent_by"].ToString(),
                            ModiBy = reader["modi_by"] == DBNull.Value ? null : reader["modi_by"].ToString(),
                            ApprBy = reader["appr_by"] == DBNull.Value ? null : reader["appr_by"].ToString(),
                            TranStatus = reader["tranStatus"] == DBNull.Value ? null : reader["tranStatus"].ToString(),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}