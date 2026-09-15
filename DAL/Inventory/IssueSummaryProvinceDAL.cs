using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class IssueSummaryProvinceDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<IssueSummaryProvinceModel> GetIssueSummaryProvince(
            DateTime fromDate, DateTime toDate, string compId, string matCode)
        {
            var result = new List<IssueSummaryProvinceModel>();

            const string query = @"
                SELECT  T1.mat_cd,
                        T2.mat_nm,
                        (SELECT comp_nm FROM glcompm WHERE comp_id = d.comp_id) AS c8,
                        T1.dept_id AS dept_id,
                        (SUM(CASE WHEN T1.add_deduct = 'F' THEN T1.trx_qty
                                  WHEN T1.add_deduct = 'T' THEN -T1.trx_qty
                                  ELSE 0.00 END)) AS commited_cost,
                        (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS comp_name
                FROM      inpostmt T1, inmatm T2, intrhmt T3, gldeptm d
                WHERE     T1.doc_no = T3.doc_no
                  AND     T1.doc_pf = T3.doc_pf
                  AND     T1.dept_id = T3.dept_id
                  AND     T1.dept_id = d.dept_id
                  AND     T2.mat_cd = T1.mat_cd
                  AND     T1.dept_id IN (SELECT dept_id
                                            FROM gldeptm
                                           WHERE comp_id IN (SELECT comp_id
                                                                FROM glcompm
                                                               WHERE TRIM(parent_id) = TRIM(:compid)))
                  AND     T2.mat_cd = T1.mat_cd
                  AND     T1.trx_dt >= :fromdate
                  AND     T1.trx_dt <  :todateexcl
                  AND     (T1.trx_type IN ('ISSUE', 'IS_CAN')
                           OR (T1.trx_type IN ('RECEIPT') AND T1.doc_pf IN ('RTV', 'RTV-CL')))
                  AND     T1.mat_cd LIKE '%' || :matcode || '%'
                GROUP BY T1.mat_cd, T2.mat_nm, d.comp_id, T1.dept_id

                UNION ALL

                SELECT  T1.mat_cd,
                        T2.mat_nm,
                        (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS c8,
                        T3.des_dept_id AS dept_id,
                        (SUM(CASE WHEN T1.add_deduct = 'F' THEN T1.trx_qty
                                  WHEN T1.add_deduct = 'T' THEN -T1.trx_qty
                                  ELSE 0.00 END)) AS commited_cost,
                        (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS comp_name
                FROM      inpostmt T1, inmatm T2, intrhmt T3
                WHERE     T1.doc_no = T3.doc_no
                  AND     T1.doc_pf = T3.doc_pf
                  AND     T1.dept_id = T3.dept_id
                  AND     T2.mat_cd = T1.mat_cd
                  AND     T3.des_dept_id IN (SELECT dept_id
                                                FROM gldeptm
                                               WHERE comp_id IN (SELECT comp_id
                                                                    FROM glcompm
                                                                   WHERE TRIM(comp_id) = TRIM(:compid)))
                  AND     T2.mat_cd = T1.mat_cd
                  AND     T1.trx_dt >= :fromdate
                  AND     T1.trx_dt <  :todateexcl
                  AND     (T1.trx_type IN ('ISSUE', 'IS_CAN')
                           OR (T1.trx_type IN ('RECEIPT') AND T1.doc_pf IN ('RTV', 'RTV-CL')))
                  AND     T1.mat_cd LIKE '%' || :matcode || '%'
                  AND     T3.des_dept_id NOT IN ('540.11', '540.00')
                GROUP BY T1.mat_cd, T2.mat_nm, T3.des_dept_id

                ORDER BY 1, 2, 3, 4";

            string compIdTrimmed = (compId ?? string.Empty).Trim();
            string matCodeTrimmed = (matCode ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :compid / :fromdate / :todateexcl / :matcode (each used multiple times) bind correctly by name

                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compIdTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });
                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2) { Value = matCodeTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new IssueSummaryProvinceModel
                        {
                            MatCd = reader["mat_cd"] == DBNull.Value ? null : reader["mat_cd"].ToString(),
                            MatNm = reader["mat_nm"] == DBNull.Value ? null : reader["mat_nm"].ToString(),
                            DeptCompName = reader["c8"] == DBNull.Value ? null : reader["c8"].ToString(),
                            DeptId = reader["dept_id"] == DBNull.Value ? null : reader["dept_id"].ToString(),
                            CommitedQty = reader["commited_cost"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["commited_cost"]),
                            CompName = reader["comp_name"] == DBNull.Value ? null : reader["comp_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}