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
    public class AreaWiseCashBookInquiryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<AreaWiseCashBookInquiryModel> GetAreaWiseCashBookInquiry(string fromDate, string toDate, string compId)
        {
            var result = new List<AreaWiseCashBookInquiryModel>();

            // NOTE: the original SQL's ORDER BY had no columns specified.
            // Defaulted to date then doc number - confirm the intended sort with your team.
            const string query = @"
                SELECT A.doc_no,
                       A.doc_dt,
                       B.exp_cd,
                       B.sub_ac,
                       B.dr_amt,
                       B.cr_amt,
                       A.non_taxabl,
                       (CASE
                            WHEN A.status = 1 THEN 'New'
                            WHEN A.status = 2 THEN 'Send for Approval'
                            WHEN A.status = 3 THEN 'Approved'
                            WHEN A.status = 4 THEN 'Transfer to GL'
                            WHEN A.status = 6 THEN 'To be cancelled'
                            WHEN A.status = 5 THEN 'Cancelled  Record'
                            WHEN A.status = 7 THEN 'Payment Plan generated '
                            WHEN A.status = 8 THEN 'PP'
                            ELSE NULL
                        END) AS transtatus,
                       A.payee,
                       A.remarks,
                       (SELECT comp_nm FROM glcompm WHERE comp_id = :compid) AS cct_name
                FROM cbpmthmt A, cbpmtett B
                WHERE A.doc_dt >= TO_DATE(:fromdate, 'yyyy/mm/dd')
                  AND A.doc_dt <= TO_DATE(:todate, 'yyyy/mm/dd')
                  AND A.dept_id IN (SELECT dept_id FROM gldeptm WHERE comp_id = :compid)
                  AND A.doc_no = B.doc_no
                  AND A.doc_pf = B.doc_pf
                  AND A.dept_id = B.dept_id
                  AND B.exp_cd <> 'L9001'
                ORDER BY A.doc_dt, A.doc_no";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :compid (used 2x) binds correctly by name
                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compId });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Varchar2) { Value = fromDate });
                cmd.Parameters.Add(new OracleParameter("todate", OracleDbType.Varchar2) { Value = toDate });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new AreaWiseCashBookInquiryModel
                        {
                            DocNo = reader["doc_no"] == DBNull.Value ? null : reader["doc_no"].ToString(),
                            DocDt = reader["doc_dt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["doc_dt"]),
                            ExpCd = reader["exp_cd"] == DBNull.Value ? null : reader["exp_cd"].ToString(),
                            SubAc = reader["sub_ac"] == DBNull.Value ? null : reader["sub_ac"].ToString(),
                            DrAmt = GetSafeDecimal(reader, "dr_amt"),
                            CrAmt = GetSafeDecimal(reader, "cr_amt"),
                            NonTaxabl = GetSafeDecimal(reader, "non_taxabl"),
                            TranStatus = reader["transtatus"] == DBNull.Value ? null : reader["transtatus"].ToString(),
                            Payee = reader["payee"] == DBNull.Value ? null : reader["payee"].ToString(),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
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