using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class PriceVaWHDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PriceVaWHModel> GetPriceVaWH(string repYear, string repMonth, string costCtr, string whCode)
        {
            var result = new List<PriceVaWHModel>();
            const string query = @"
                SELECT T1.wrh_cd, T1.mat_cd, T1.grade_cd, T1.unit_price, T1.new_price, T1.net_change,
                       T1.qty_on_hand,
                       (T1.net_change * T1.qty_on_hand) AS VAR,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM inadjdmt T1, inadjbtm T2, inadjhmt T3
                WHERE T1.doc_no = T3.doc_no
                  AND T1.doc_pf = T3.doc_pf
                  AND T1.dept_id = T3.dept_id
                  AND T1.wrh_cd = T3.wrh_cd
                  AND T2.doc_pf = T3.doc_pf
                  AND T2.dept_id = T3.dept_id
                  AND T2.batch_id = T3.batch_id
                  AND T3.status IN (4)
                  AND TRIM(T2.dept_id) = :costctr
                  AND T2.fin_yr = :repyear
                  AND T2.fin_mth = :repmonth
                  AND UPPER(TRIM(T1.wrh_cd)) = UPPER(:whcode)
                  AND T1.adj_type = 'PRICE'
                  AND T1.net_change <> 0.00
                ORDER BY T1.wrh_cd, T1.mat_cd";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Varchar2) { Value = repYear });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Varchar2) { Value = repMonth });
                cmd.Parameters.Add(new OracleParameter("whcode", OracleDbType.Varchar2) { Value = whCode });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new PriceVaWHModel
                        {
                            WrhCd = reader["wrh_cd"] == DBNull.Value ? null : reader["wrh_cd"].ToString(),
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            GradeCd = reader["grade_cd"] == DBNull.Value ? null : reader["grade_cd"].ToString(),
                            UnitPrice = reader["unit_price"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["unit_price"]),
                            NewPrice = reader["new_price"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["new_price"]),
                            NetChange = reader["net_change"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["net_change"]),
                            QtyOnHand = reader["qty_on_hand"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["qty_on_hand"]),
                            Var = reader["VAR"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["VAR"]),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}