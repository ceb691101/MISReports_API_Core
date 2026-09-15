using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class BranchGrnNotGenDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<BranchGrnNotGenModel> GetBranchGrnNotGen(string fromDate, string toDate, string compId)
        {
            var result = new List<BranchGrnNotGenModel>();

            // Escape single quotes in compId to prevent SQL injection
            string safeCompId = compId.Replace("'", "''");

            string query = $@"
        SELECT T2.doc_no, T2.trx_dt, T2.des_dept_id, T2.wrh_cd, T2.ref_1, T2.trxn_val,
               (SELECT comp_nm FROM glcompm WHERE comp_id = '{safeCompId}') AS CCT_NAME
        FROM intrhmt T2
        WHERE T2.Issue_to = 2
          AND T2.dept_id IN (
              SELECT dept_id
              FROM gldeptm
              WHERE status = 2
                AND comp_id IN (
                    SELECT comp_id
                    FROM glcompm
                    WHERE comp_id = '{safeCompId}'
                       OR parent_id = '{safeCompId}'
                       OR grp_comp = '{safeCompId}'
                )
          )
          AND TO_DATE(:fromdate,'yyyy/mm/dd') <= T2.trx_dt
          AND TO_DATE(:todate,'yyyy/mm/dd') >= T2.trx_dt
          AND TRIM(T2.doc_no) NOT IN (
              SELECT TRIM(ref_2)
              FROM intrhmt
              WHERE rc_from = 3
                AND dept_id = T2.des_dept_id
          )";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new BranchGrnNotGenModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            TrxDt = reader["trx_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["trx_dt"]),
                            DesDeptId = reader["des_dept_id"] == DBNull.Value ? null : reader["des_dept_id"].ToString(),
                            WrhCd = reader["wrh_cd"] == DBNull.Value ? null : reader["wrh_cd"].ToString(),
                            Ref1 = reader["ref_1"] == DBNull.Value ? null : reader["ref_1"].ToString(),
                            TrxnVal = reader["trxn_val"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["trxn_val"]),
                            BranchName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}