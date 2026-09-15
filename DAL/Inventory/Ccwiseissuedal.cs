using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.DAL
{
    public class CCWiseIssueDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        public List<CCWiseIssueModel> GetCCWiseIssue(int repYear, int repMonth, string costCtr)
        {
            var result = new List<CCWiseIssueModel>();
            const string query = @"
                SELECT (CASE WHEN T2.Is_ref LIKE '%SMC%' THEN '1SMC'
                             WHEN T2.Is_ref LIKE '%CR%' THEN '2CR'
                             WHEN T2.Is_ref LIKE '%MAIN%' THEN '3MAINTAINENCE'
                             WHEN T2.Is_ref LIKE '%BDJ%' THEN '4BDJ'
                             ELSE '5OTHER' END) AS Type,
                       T1.yr_ind,
                       T1.mth_ind,
                       T1.trx_type,
                       T2.trx_dt,
                       T1.doc_pf,
                       T1.doc_no,
                       T2.ref_1,
                       T2.ref_2,
                       T2.ref_3,
                       T2.ref_4,
                       SUM(T1.trx_val) AS total,
                       SUBSTR(T2.remarks, 1, 50) AS remarks,
                       T2.Is_ref,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
                FROM inpostmt T1, intrhmt T2
                WHERE T1.doc_no = T2.doc_no
                  AND T1.doc_pf = T2.doc_pf
                  AND T1.dept_id = T2.dept_id
                  AND T1.dept_id = :costctr
                  AND T1.yr_ind = :repyear
                  AND T1.mth_ind = :repmonth
                  AND T1.trx_type IN ('ISSUE', 'IS_CAN')
                GROUP BY 1, T1.trx_type, T1.doc_pf, T1.doc_no, T1.yr_ind, T1.mth_ind, T2.ref_1,
                         T2.Remarks, T2.trx_dt, T2.Is_ref, T2.ref_2, T2.ref_3, T2.ref_4
                ORDER BY (CASE WHEN T2.Is_ref LIKE '%SMC%' THEN '1SMC'
                               WHEN T2.Is_ref LIKE '%CR%' THEN '2CR'
                               WHEN T2.Is_ref LIKE '%MAIN%' THEN '3MAINTAINENCE'
                               WHEN T2.Is_ref LIKE '%BDJ%' THEN '4BDJ'
                               ELSE '5OTHER' END),
                         T1.trx_type, T1.doc_pf, T2.trx_dt, T1.doc_no, T1.yr_ind, T1.mth_ind";
            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Int32) { Value = repYear });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Int32) { Value = repMonth });
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CCWiseIssueModel
                        {
                            Type = reader["Type"] == DBNull.Value ? null : reader["Type"].ToString(),
                            YrInd = reader["yr_ind"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["yr_ind"]),
                            MthInd = reader["mth_ind"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["mth_ind"]),
                            TrxType = reader["trx_type"] == DBNull.Value ? null : reader["trx_type"].ToString(),
                            TrxDt = reader["trx_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["trx_dt"]),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            Ref1 = reader["ref_1"] == DBNull.Value ? null : reader["ref_1"].ToString(),
                            Ref2 = reader["ref_2"] == DBNull.Value ? null : reader["ref_2"].ToString(),
                            Ref3 = reader["ref_3"] == DBNull.Value ? null : reader["ref_3"].ToString(),
                            Ref4 = reader["ref_4"] == DBNull.Value ? null : reader["ref_4"].ToString(),
                            Total = reader["total"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["total"]),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            IsRef = reader["Is_ref"] == DBNull.Value ? null : reader["Is_ref"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }
            return result;
        }
    }
}