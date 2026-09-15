using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class CashSheetDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CashSheetModel> GetCashSheet(string repYear, string repMonth, string costCtr)
        {
            var result = new List<CashSheetModel>();

            const string query = @"
                SELECT DISTINCT T1.chq_run,
                                T1.chq_dt,
                                T1.payee,
                                T1.pymt_docno,
                                T1.chq_amt,
                                T1.chq_no,
                                (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM cbchqdmt T1, cbchqhmt T2, cbchqrgh T3
                WHERE TO_CHAR(T1.chq_dt, 'YYYY') = :repyear
                  AND TO_CHAR(T1.chq_dt, 'MM') = :repmonth
                  AND T1.dept_id = :costctr
                  AND T2.status NOT IN (8)
                  AND T3.status NOT IN (3)
                  AND T1.dept_id = T2.dept_id
                  AND T1.chq_no = T3.chq_no
                  AND T1.chq_run = T2.chq_run
                  AND T1.chq_run = T3.chq_run
                  AND T3.chq_run = T2.chq_run
                ORDER BY T1.chq_run, T1.chq_no, T1.chq_dt";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Varchar2) { Value = repYear });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Varchar2) { Value = repMonth });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CashSheetModel
                        {
                            ChqRun = reader["chq_run"] == DBNull.Value ? null : reader["chq_run"].ToString(),
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            PymtDocNo = reader["pymt_docno"] == DBNull.Value ? null : reader["pymt_docno"].ToString(),
                            ChqAmt = reader["chq_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["chq_amt"]),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}