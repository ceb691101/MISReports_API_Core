using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using ChqApp.Models;

namespace ChqApp.DAL
{
   public class CashSheetDateRangePayeeDAL
    {
        private readonly string _connectionString;

        public CashSheetDateRangePayeeDAL(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<CashSheetDateRangePayeeModel> GetCashSheetDateRangePayeeModel(string costCtr, DateTime fromDate, DateTime toDate, string payee)
        {
            var results = new List<CashSheetDateRangePayeeModel>();
            bool hasPayeeFilter = !string.IsNullOrWhiteSpace(payee);

            string query = @"
                SELECT T1.chq_run,
                       T1.chq_dt,
                       T1.payee,
                       T1.pymt_docno,
                       T1.chq_amt,
                       T1.chq_no,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costCtr) AS cct_name
                FROM cbchqdmt T1
                WHERE T1.chq_dt >= :fromDate
                  AND T1.chq_dt <= :toDate
                  AND T1.dept_id = :costCtr";

            if (hasPayeeFilter)
            {
                query += " AND T1.payee LIKE '%' || TRIM(:payee) || '%'";
            }

            query += @"
                GROUP BY T1.payee, T1.chq_dt, T1.chq_no, T1.chq_run, T1.pymt_docno, T1.chq_amt
                ORDER BY T1.payee, T1.chq_dt, T1.chq_no";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.BindByName = true;

                cmd.Parameters.Add(new OracleParameter("costCtr", OracleDbType.Varchar2) { Value = costCtr ?? string.Empty });
                cmd.Parameters.Add(new OracleParameter("fromDate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("toDate", OracleDbType.Date) { Value = toDate.Date });

                if (hasPayeeFilter)
                {
                    cmd.Parameters.Add(new OracleParameter("payee", OracleDbType.Varchar2) { Value = payee.Trim() });
                }

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new CashSheetDateRangePayeeModel
                        {
                            ChqRun = reader["chq_run"] == DBNull.Value ? string.Empty : reader["chq_run"].ToString(),
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            Payee = reader["payee"] == DBNull.Value ? string.Empty : reader["payee"].ToString(),
                            PymtDocNo = reader["pymt_docno"] == DBNull.Value ? string.Empty : reader["pymt_docno"].ToString(),
                            ChqAmt = reader["chq_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["chq_amt"]),
                            ChqNo = reader["chq_no"] == DBNull.Value ? string.Empty : reader["chq_no"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? string.Empty : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return results;
        }
    }
}