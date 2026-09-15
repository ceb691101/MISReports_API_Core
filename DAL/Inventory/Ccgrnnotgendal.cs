using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class CcGrnNotGenDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CcGrnNotGenModel> GetCcGrnNotGen(string fromDate, string toDate, string costCtr)
        {
            var result = new List<CcGrnNotGenModel>();
            // NOTE: original SQL from senior hardcoded dept_id = '510.11' in the cct_name subquery.
            // Replaced with :costctr so the cost center name always matches the queried cost center.
            const string query = @"
                SELECT T2.doc_no, T2.trx_dt, T2.dept_id, T2.des_dept_id, T2.wrh_cd, T2.ref_1, T2.trxn_val,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS CCT_NAME
                FROM intrhmt T2
                WHERE T2.doc_no IN (
                    SELECT T1.doc_no
                    FROM intrnsit T1, GLTRNSIT T3
                    WHERE T1.rc_dept_id = :costctr
                      AND T1.rc_doc_no IS NULL
                      AND T3.GEN != 'T'
                      AND T3.doc_no = T1.doc_no
                      AND T1.doc_no IN (
                          SELECT doc_no
                          FROM intrhmt T2
                          WHERE TO_DATE(:fromdate,'yyyy/mm/dd') <= T2.trx_dt
                            AND TO_DATE(:todate,'yyyy/mm/dd') >= T2.trx_dt
                            AND T2.status NOT IN (4, 5)
                            AND T2.des_dept_id = :costctr
                      )
                )";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used 3 times) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CcGrnNotGenModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            TrxDt = reader["trx_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["trx_dt"]),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            DesDeptId = reader["des_dept_id"] == DBNull.Value ? null : reader["des_dept_id"].ToString(),
                            WrhCd = reader["wrh_cd"] == DBNull.Value ? null : reader["wrh_cd"].ToString(),
                            Ref1 = reader["ref_1"] == DBNull.Value ? null : reader["ref_1"].ToString(),
                            TrxnVal = reader["trxn_val"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["trxn_val"]),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}