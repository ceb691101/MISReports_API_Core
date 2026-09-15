using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class ChequeCancellationDivisionDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ChequeCancellationDivisionModel> GetChequeCancellationDivision(string fromDate, string toDate, string compId)
        {
            var result = new List<ChequeCancellationDivisionModel>();

            string query = @"
        SELECT A.dept_id, A.doc_no, A.chq_dt, A.chq_no, A.chq_amt, A.chq_run, A1.run_dt,
               (SELECT comp_nm FROM glcompm WHERE comp_id = :compid) AS CCT_NAME
        FROM cbcqchmt A, cbchqhmt A1
        WHERE TRIM(A.chq_run) = TRIM(A1.chq_run)
          AND A.dept_id IN (
              SELECT dept_id
              FROM gldeptm
              WHERE comp_id IN (
                  SELECT comp_id
                  FROM glcompm
                  WHERE comp_id = :compid
                     OR parent_id = :compid
                     OR grp_comp = :compid
              )
          )
          AND (A.chq_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
               AND A.chq_dt <= TO_DATE(:todate,'yyyy/mm/dd'))
        ORDER BY A.dept_id, A.chq_dt, A.chq_no";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compId });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ChequeCancellationDivisionModel
                        {
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            ChqAmt = reader["chq_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["chq_amt"]),
                            ChqRun = reader["chq_run"] == DBNull.Value ? null : reader["chq_run"].ToString(),
                            RunDt = reader["run_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["run_dt"]),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}