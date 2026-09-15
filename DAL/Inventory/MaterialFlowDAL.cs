using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class MaterialFlowDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<MaterialFlowModel> GetMaterialFlow(string fromDate, string toDate, string costCtr, string matCode, string grCode, string whCode)
        {
            var result = new List<MaterialFlowModel>();
            const string query = @"
                SELECT T1.doc_no,
                       T1.trx_type,
                       CASE WHEN T2.is_ref IS NOT NULL THEN T2.is_ref ELSE T2.rc_ref END AS iss_ref,
                       SUM(CASE WHEN T1.add_deduct = 'T' THEN T1.trx_qty
                                WHEN T1.add_deduct = 'F' THEN -T1.trx_qty
                                ELSE NULL END) AS add_or_sub,
                       T1.trx_dt AS tr_date,
                       T2.ref_3, T2.ref_4,
                       (CASE WHEN T1.add_deduct = 'T' THEN 1 ELSE 0 END) AS addition,
                       (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME,
                       (SELECT SUM(qty_on_hand)
                          FROM inwrhmtm
                         WHERE TRIM(wrh_cd) = TRIM(:whcode)
                           AND TRIM(dept_id) = TRIM(:costctr)
                           AND TRIM(grade_cd) = TRIM(:grcode)
                           AND TRIM(mat_cd) = TRIM(:matcode)) AS qty_on_handp,
                       (SELECT SUM(CASE WHEN (T1.add_deduct = 'T') THEN (-T1.trx_qty) ELSE 0 END)
                          FROM inpostmt T1
                         WHERE TRIM(T1.dept_id) = TRIM(:costctr)
                           AND TRIM(T1.mat_cd) = TRIM(:matcode)
                           AND TRIM(T1.grade_cd) = TRIM(:grcode)
                           AND T1.trx_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                           AND TRIM(T1.wrh_cd) = TRIM(:whcode)
                           AND NOT (T1.trx_type IN ('PRICE_ADJ'))) AS q_in,
                       (SELECT SUM(CASE WHEN (T1.add_deduct = 'F') THEN (T1.trx_qty) ELSE 0 END)
                          FROM inpostmt T1
                         WHERE TRIM(T1.dept_id) = TRIM(:costctr)
                           AND TRIM(T1.mat_cd) = TRIM(:matcode)
                           AND TRIM(T1.grade_cd) = TRIM(:grcode)
                           AND T1.trx_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                           AND TRIM(T1.wrh_cd) = TRIM(:whcode)
                           AND NOT (T1.trx_type IN ('PRICE_ADJ'))) AS q_out,
                       (SELECT SUM(CASE WHEN (T1.add_deduct = 'T') THEN (-T1.trx_qty) ELSE 0 END)
                          FROM inpostmt T1
                         WHERE TRIM(T1.dept_id) = TRIM(:costctr)
                           AND TRIM(T1.mat_cd) = TRIM(:matcode)
                           AND TRIM(T1.grade_cd) = TRIM(:grcode)
                           AND T1.trx_dt > TO_DATE(:todate,'yyyy/mm/dd')
                           AND TRIM(T1.wrh_cd) = TRIM(:whcode)
                           AND NOT (T1.trx_type IN ('PRICE_ADJ'))) AS cin,
                       (SELECT SUM(CASE WHEN (T1.add_deduct = 'F') THEN (T1.trx_qty) ELSE 0 END)
                          FROM inpostmt T1
                         WHERE TRIM(T1.dept_id) = TRIM(:costctr)
                           AND TRIM(T1.mat_cd) = TRIM(:matcode)
                           AND TRIM(T1.grade_cd) = TRIM(:grcode)
                           AND T1.trx_dt > TO_DATE(:todate,'yyyy/mm/dd')
                           AND TRIM(T1.wrh_cd) = TRIM(:whcode)
                           AND NOT (T1.trx_type IN ('PRICE_ADJ'))) AS cout
                FROM (inpostmt T1 LEFT OUTER JOIN intrhmt T2
                        ON T1.doc_no = T2.doc_no
                       AND T1.doc_pf = T2.doc_pf
                       AND T1.dept_id = T2.dept_id
                       AND TRIM(T1.dept_id) = TRIM(:costctr))
                WHERE TRIM(T1.dept_id) = TRIM(:costctr)
                  AND TRIM(T1.mat_cd) = TRIM(:matcode)
                  AND TRIM(T1.grade_cd) = TRIM(:grcode)
                  AND TRIM(T1.wrh_cd) = TRIM(:whcode)
                  AND NOT (T1.trx_type IN ('PRICE_ADJ'))
                  AND T1.trx_dt >= TO_DATE(:fromdate,'yyyy/mm/dd')
                  AND T1.trx_dt <= TO_DATE(:todate,'yyyy/mm/dd')
                GROUP BY T1.doc_no, T1.trx_type, T1.trx_dt, T1.add_deduct,
                         CASE WHEN T2.is_ref IS NOT NULL THEN T2.is_ref ELSE T2.rc_ref END,
                         T2.ref_3, T2.ref_4
                ORDER BY 5 ASC, 1 ASC, 2 ASC";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so repeated named params bind correctly
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2) { Value = matCode });
                cmd.Parameters.Add(new OracleParameter("grcode", OracleDbType.Varchar2) { Value = grCode });
                cmd.Parameters.Add(new OracleParameter("whcode", OracleDbType.Varchar2) { Value = whCode });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new MaterialFlowModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            TrxType = reader["trx_type"] == DBNull.Value ? null : reader["trx_type"].ToString(),
                            IssRef = reader["iss_ref"] == DBNull.Value ? null : reader["iss_ref"].ToString(),
                            AddOrSub = reader["add_or_sub"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["add_or_sub"]),
                            TrxDate = reader["tr_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["tr_date"]),
                            Ref3 = reader["ref_3"] == DBNull.Value ? null : reader["ref_3"].ToString(),
                            Ref4 = reader["ref_4"] == DBNull.Value ? null : reader["ref_4"].ToString(),
                            Addition = reader["addition"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["addition"]),
                            CctName = reader["CCT_NAME"] == DBNull.Value ? null : reader["CCT_NAME"].ToString(),
                            QtyOnHandP = reader["qty_on_handp"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["qty_on_handp"]),
                            QIn = reader["q_in"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["q_in"]),
                            QOut = reader["q_out"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["q_out"]),
                            CIn = reader["cin"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["cin"]),
                            COut = reader["cout"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["cout"])
                        });
                    }
                }
            }
            return result;
        }

        public List<string> GetDistinctGradeCodes()
        {
            var result = new List<string>();
            const string query = "SELECT DISTINCT(grade_cd) FROM inwrhmtm ORDER BY grade_cd";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["grade_cd"] != DBNull.Value)
                            result.Add(reader["grade_cd"].ToString());
                    }
                }
            }
            return result;
        }
    }
}