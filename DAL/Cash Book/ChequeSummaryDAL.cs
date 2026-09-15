using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class ChequeSummaryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ChequeSummaryModel> GetChequeSummary(string fromDate, string toDate, string costCtr)
        {
            var result = new List<ChequeSummaryModel>();
            
            const string query = @"
                SELECT DISTINCT A.chq_dt, A.chq_no, B.chq_amt,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM cbchqdmt A, cbchqrgh B
                WHERE TRIM(A.chq_run) = TRIM(B.chq_run)
                  AND B.status NOT IN (3)
                  AND TRIM(A.chq_no) = TRIM(B.chq_no)
                  AND A.dept_id = :costctr
                  AND (A.chq_dt >= TO_DATE(:fromdate,'yyyy/mm/dd') AND A.chq_dt <= TO_DATE(:todate,'yyyy/mm/dd'))
                ORDER BY A.chq_no, A.chq_dt";

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
                        result.Add(new ChequeSummaryModel
                        {
                            ChqDt = reader["chq_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["chq_dt"]),
                            ChqNo = reader["chq_no"] == DBNull.Value ? null : reader["chq_no"].ToString(),
                            ChqAmt = reader["chq_amt"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["chq_amt"]),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}