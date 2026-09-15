using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class InquiryChequeRunDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<InquiryChequeRunModel> GetInquiryChequeRun(string fromDate, string toDate, string costCtr)
        {
            var result = new List<InquiryChequeRunModel>();

            string query = @"
        SELECT 'Cheque - Temporary Transcation' AS Category, chq_run, doc_pf, run_dt, run_by,
               modi_by, apprv_uid1,
               (CASE WHEN status = 1 THEN 'Create Payment Plan'
                     WHEN status = 3 THEN 'Print PP Final report'
                     WHEN status = 4 THEN 'Edit Payment  Plan'
                     WHEN status = 6 THEN 'Send for second approval'
                     WHEN status = 5 THEN 'Send  PP for Approval'
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM cbchqhtt
        WHERE NOT (status = 0)
          AND dept_id = :costctr
          AND run_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND run_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        UNION ALL
        SELECT 'Cheque Payment Transcation' AS Category, chq_run, doc_pf, run_dt, run_by,
               modi_by, apprv_uid1,
               (CASE WHEN status = 1 THEN 'Approved  Payment Plan'
                     WHEN status = 3 THEN 'Cheque printed'
                     WHEN status = 5 THEN 'Transfer to GL'
                     WHEN status = 7 THEN 'Cheque assignment Report'
                     WHEN status = 8 THEN 'Confirmation'
                     ELSE NULL END) AS tranStatus,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM cbchqhmt
        WHERE NOT (status = 6)
          AND dept_id = :costctr
          AND run_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND run_dt <= TO_DATE(:todate,'yyyy/mm/dd')
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
                        result.Add(new InquiryChequeRunModel
                        {
                            Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                            ChqRun = reader["chq_run"] == DBNull.Value ? null : reader["chq_run"].ToString(),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            RunDt = reader["run_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["run_dt"]),
                            RunBy = reader["run_by"] == DBNull.Value ? null : reader["run_by"].ToString(),
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