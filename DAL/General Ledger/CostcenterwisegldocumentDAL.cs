using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class CostCenterWiseGLDocumentDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CostCenterWiseGLDocumentModel> GetCostCenterWiseGLDocuments(string fromDate, string toDate, string costCtr)
        {
            var result = new List<CostCenterWiseGLDocumentModel>();

            // NOTE: the Temp block's original CASE had a duplicate WHEN a.status=6 branch
            // ('Printed' then 'Rejected') - Oracle CASE resolves top-down, so the second
            // branch was unreachable dead code. Dropped here; behaviorally identical to
            // what the original SQL actually executed. Flag with your team if status 6
            // was meant to mean 'Rejected' somewhere instead.
            const string query = @"
                SELECT 'GL-After Posting' AS category,
                       A.doc_no,
                       A.doc_pf,
                       A.doc_dt,
                       A.ent_by,
                       A.modi_by,
                       A.appr_by,
                       B.dr_amt,
                       B.cr_amt,
                       B.gl_cd,
                       B.sub_ac,
                       A.remarks,
                       A.trx_val,
                       (CASE
                            WHEN A.status = 1 THEN 'New'
                            WHEN A.status = 2 THEN 'Confirmed'
                            WHEN A.status = 6 THEN 'GL Posted'
                            ELSE NULL
                        END) AS transtatus
                FROM glvochmt A, glvocdmt B
                WHERE A.doc_no = B.doc_no
                  AND A.batch_id = B.batch_id
                  AND A.doc_pf = B.doc_pf
                  AND A.dept_id = B.dept_id
                  AND A.dept_id = :costctr
                  AND A.doc_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                  AND A.doc_dt <= TO_DATE(:todate, 'yyyy/mm/dd')

                UNION ALL

                SELECT 'GL- Temporary Transcations' AS category,
                       A.doc_no,
                       A.doc_pf,
                       A.doc_dt,
                       A.ent_by,
                       A.modi_by,
                       A.appr_by,
                       B.dr_amt,
                       B.cr_amt,
                       B.gl_cd,
                       B.sub_ac,
                       A.remarks,
                       A.trx_val,
                       (CASE
                            WHEN A.status = 1 THEN 'New'
                            WHEN A.status = 2 THEN 'Confirmed'
                            WHEN A.status = 3 THEN 'Edit Batch'
                            WHEN A.status = 4 THEN 'Edited Batch'
                            WHEN A.status = 5 THEN 'Generated'
                            WHEN A.status = 6 THEN 'Printed'
                            ELSE NULL
                        END) AS transtatus
                FROM glvochtt A, glvocdtt B
                WHERE NOT (A.status = 0)
                  AND A.doc_no = B.doc_no
                  AND A.batch_id = B.batch_id
                  AND A.doc_pf = B.doc_pf
                  AND A.dept_id = B.dept_id
                  AND A.dept_id = :costctr
                  AND A.doc_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                  AND A.doc_dt <= TO_DATE(:todate, 'yyyy/mm/dd')

                ORDER BY 2, 1, 3, 4";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used 2x) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CostCenterWiseGLDocumentModel
                        {
                            Category = reader["category"] == DBNull.Value ? null : reader["category"].ToString(),
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocPf = reader["doc_pf"] == DBNull.Value ? null : reader["doc_pf"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            EntBy = reader["ent_by"] == DBNull.Value ? null : reader["ent_by"].ToString(),
                            ModiBy = reader["modi_by"] == DBNull.Value ? null : reader["modi_by"].ToString(),
                            ApprBy = reader["appr_by"] == DBNull.Value ? null : reader["appr_by"].ToString(),
                            DrAmt = GetSafeDecimal(reader, "dr_amt"),
                            CrAmt = GetSafeDecimal(reader, "cr_amt"),
                            GlCd = reader["gl_cd"] == DBNull.Value ? null : reader["gl_cd"].ToString(),
                            SubAc = reader["sub_ac"] == DBNull.Value ? null : reader["sub_ac"].ToString(),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            TrxVal = GetSafeDecimal(reader, "trx_val"),
                            TranStatus = reader["transtatus"] == DBNull.Value ? null : reader["transtatus"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Safely reads a NUMBER column as a nullable decimal.
        /// Oracle's NUMBER type can carry more precision than .NET's decimal can hold,
        /// which makes a plain Convert.ToDecimal(reader[...]) throw OverflowException
        /// on some rows. Reading via OracleDecimal and clamping precision first avoids that.
        /// </summary>
        private static decimal? GetSafeDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;

            OracleDecimal oraVal = reader.GetOracleDecimal(ordinal);
            oraVal = OracleDecimal.SetPrecision(oraVal, 28);

            return oraVal.Value;
        }
    }
}