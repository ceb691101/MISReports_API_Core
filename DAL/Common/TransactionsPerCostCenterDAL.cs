using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class TransactionsPerCostCenterDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<TransactionsPerCostCenterModel> GetTransactionsPerCostCenter(string fromDate, string toDate, string costCtr)
        {
            var result = new List<TransactionsPerCostCenterModel>();

            string query = @"
        SELECT 'STORES' AS Category, trx_dt AS Entered_date, COUNT(*) AS count,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM intrhmt
        WHERE dept_id = :costctr
          AND trx_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND trx_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        GROUP BY trx_dt
        UNION ALL
        SELECT 'REQUISITION & RETURNS' AS Category, ent_dt AS Entered_date, COUNT(*) AS count,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM mtreqhtt
        WHERE dept_id = :costctr
          AND ent_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND ent_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        GROUP BY ent_dt
        UNION ALL
        SELECT 'PAYSLIPS' AS Category, doc_dt AS Entered_date, COUNT(*) AS count,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM cbpmthtt
        WHERE dept_id = :costctr
          AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        GROUP BY doc_dt
        UNION ALL
        SELECT 'CHEQUE RUN' AS Category, run_dt AS Entered_date, COUNT(*) AS count,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM cbchqhtt
        WHERE dept_id = :costctr
          AND run_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND run_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        GROUP BY run_dt
        UNION ALL
        SELECT 'GENERAL LEDGER' AS Category, doc_dt AS Entered_date, COUNT(*) AS count,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM glvochtt
        WHERE dept_id = :costctr
          AND doc_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND doc_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        GROUP BY doc_dt
        UNION ALL
        SELECT 'ESTIMATES' AS Category, etimate_dt AS Entered_date, COUNT(DISTINCT estimate_no) AS count,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM pcesthtt
        WHERE dept_id = :costctr
          AND etimate_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND etimate_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        GROUP BY etimate_dt
        UNION ALL
        SELECT 'JOB NO ASSIGNED' AS Category, ent_dt AS Entered_date, COUNT(*) AS count,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM pcesthmt
        WHERE dept_id = :costctr
          AND ent_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND ent_dt <= TO_DATE(:todate,'yyyy/mm/dd')
        GROUP BY ent_dt
        UNION ALL
        SELECT 'COMMERCIAL' AS Category, entry_date AS Entered_date, COUNT(*) AS count,
               (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
        FROM spstdesthmt
        WHERE dept_id = :costctr
          AND entry_date >= TO_DATE(:fromdate,'yyyy/mm/dd')
          AND entry_date <= TO_DATE(:todate,'yyyy/mm/dd')
        GROUP BY entry_date
        ORDER BY 1, 2";

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
                        result.Add(new TransactionsPerCostCenterModel
                        {
                            Category = reader["Category"] == DBNull.Value ? null : reader["Category"].ToString(),
                            EnteredDate = reader["Entered_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Entered_date"]),
                            Count = reader["count"] == DBNull.Value ? 0 : Convert.ToInt32(reader["count"]),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}