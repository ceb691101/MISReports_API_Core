using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class InquiryCashBookDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<InquiryCashBookModel> GetInquiryCashBook(string fromDate, string toDate, string costCtr)
        {
            var result = new List<InquiryCashBookModel>();

            string query = @"
        SELECT 'Cash Book- After Posting' AS Category, doc_no, doc_pf, doc_dt, ent_by, modi_by, apprv_uid1,
               (CASE WHEN status = 1 THEN 'New'
                     WHEN status = 2 THEN 'Send for Approval'
                     WHEN status = 3 THEN 'Approved'
                     WHEN status = 6 THEN 'To be cancelled'
                     WHEN status = 5 THEN 'Cancelled  Record'
                     WHEN status = 7 THEN 'Payment Plan generated '
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM cbpmthmt
        WHERE NOT (status = 4 OR status = 8)
          AND dept_id = :costctr
          AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        UNION ALL
        SELECT 'Cash Book-Temp Transcation' AS Category, doc_no, doc_pf, doc_dt, ent_by, modi_by, apprv_uid1,
               (CASE WHEN status = 1 THEN 'New Record'
                     WHEN status = 2 THEN 'Send for 1st. Approval'
                     WHEN status = 3 THEN 'Approved'
                     WHEN status = 4 THEN 'Rejected'
                     WHEN status = 6 THEN 'Send for second approval'
                     WHEN status = 7 THEN 'Approved Once'
                     WHEN status = 8 THEN 'Printed '
                     WHEN status = 9 THEN 'Cancelled'
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM cbpmthtt
        WHERE NOT (status = 0)
          AND dept_id = :costctr
          AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        ORDER BY 8, 1, 3, 2, 4";

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
                        result.Add(new InquiryCashBookModel
                        {
                            Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            EntBy = reader["ent_by"] == DBNull.Value ? null : reader["ent_by"].ToString(),
                            ModiBy = reader["modi_by"] == DBNull.Value ? null : reader["modi_by"].ToString(),
                            ApprvUid1 = reader["apprv_uid1"] == DBNull.Value ? null : reader["apprv_uid1"].ToString(),
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