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
    public class ConstructionCompletedDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ConstructionCompletedModel> GetConstructionCompleted(string costCtr, string fundId, string district, string csc)
        {
            var result = new List<ConstructionCompletedModel>();

            const string query = @"
                SELECT A.district,
                       A.servicedeponame,
                       A.electorate,
                       B.descr,
                       B.std_cost,
                       C.cpercentage,
                       ((B.std_cost * C.cpercentage) / 100) AS wp,
                       B.project_no,
                       A.file_no,
                       D.remarks,
                       (SELECT MAX(d1.enter_date)
                        FROM pcmiledates d1
                        WHERE TRIM(A.file_no) = TRIM(d1.project_no)
                          AND d1.dept_id = A.dept_id
                          AND TRIM(d1.cpercentage) = 88) AS comp_date,
                       (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_nm
                FROM pcinitialdata A, pcmilesumary C, pcesthmt B, pcmiledates D
                WHERE A.dept_id = B.dept_id
                  AND A.dept_id = C.dept_id
                  AND TRIM(A.project_no) = TRIM(B.project_no)
                  AND TRIM(A.file_no) = TRIM(C.project_no)
                  AND TRIM(A.fund_id) = TRIM(B.fund_id)
                  AND B.dept_id = C.dept_id
                  AND B.dept_id = D.dept_id
                  AND TRIM(B.estimate_no) = TRIM(C.project_no)
                  AND TRIM(B.estimate_no) = TRIM(D.project_no)
                  AND C.mile_id = D.mile_id
                  AND C.cpercentage = (
                        SELECT DISTINCT d1.cpercentage
                        FROM pcmiledates d1
                        WHERE TRIM(d1.project_no) = TRIM(C.project_no)
                          AND d1.dept_id = C.dept_id
                          AND d1.mile_id = C.mile_id
                          AND TRIM(d1.cpercentage) = TRIM(C.cpercentage)
                      )
                  AND (
                        SELECT MAX(d1.enter_date)
                        FROM pcmiledates d1
                        WHERE TRIM(d1.project_no) = TRIM(C.project_no)
                          AND d1.dept_id = C.dept_id
                          AND d1.mile_id = C.mile_id
                          AND d1.cpercentage = C.cpercentage
                      ) = D.enter_date
                  AND C.dept_id = D.dept_id
                  AND TRIM(D.project_no) = TRIM(C.project_no)
                  AND TRIM(B.fund_id) LIKE '%' || :fundid || '%'
                  AND TRIM(A.district) LIKE '%' || TRIM(:district) || '%'
                  AND TRIM(A.servicedeponame) LIKE '%' || TRIM(:csc) || '%'
                  AND B.dept_id = :costctr
                  AND (C.cpercentage >= 88 AND C.cpercentage <= 100)
                ORDER BY A.district, A.servicedeponame, A.electorate, B.project_no";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (3x) / :fundid (1x) bind correctly by name
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fundid", OracleDbType.Varchar2) { Value = fundId });
                cmd.Parameters.Add(new OracleParameter("district", OracleDbType.Varchar2) { Value = district });
                cmd.Parameters.Add(new OracleParameter("csc", OracleDbType.Varchar2) { Value = csc });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ConstructionCompletedModel
                        {
                            District = reader["district"] == DBNull.Value ? null : reader["district"].ToString(),
                            ServiceDepoName = reader["servicedeponame"] == DBNull.Value ? null : reader["servicedeponame"].ToString(),
                            Electorate = reader["electorate"] == DBNull.Value ? null : reader["electorate"].ToString(),
                            Descr = reader["descr"] == DBNull.Value ? null : reader["descr"].ToString(),
                            StdCost = GetSafeDecimal(reader, "std_cost"),
                            CPercentage = GetSafeDecimal(reader, "cpercentage"),
                            Wp = GetSafeDecimal(reader, "wp"),
                            ProjectNo = reader["project_no"] == DBNull.Value ? null : reader["project_no"].ToString(),
                            FileNo = reader["file_no"] == DBNull.Value ? null : reader["file_no"].ToString(),
                            Remarks = reader["remarks"] == DBNull.Value ? null : reader["remarks"].ToString(),
                            CompDate = reader["comp_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["comp_date"]),
                            CctName = reader["cct_nm"] == DBNull.Value ? null : reader["cct_nm"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Counts jobs still in progress (cpercentage &lt; 88) for the given cost center / fund.
        /// Pulled out as its own query since this figure is row-invariant (doesn't depend on
        /// the main result set), so it stays correct even when zero completed jobs are returned.
        /// </summary>
        public int GetInProgressCount(string costCtr, string fundId)
        {
            const string query = @"
                SELECT COUNT(c1.project_no) AS in_progress_count
                FROM pcmilesumary c1, pcesthmt b1
                WHERE c1.cpercentage < 88
                  AND c1.dept_id = :costctr
                  AND c1.dept_id = b1.dept_id
                  AND b1.fund_id LIKE '%' || :fundid || '%'
                  AND TRIM(b1.estimate_no) = TRIM(c1.project_no)";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtr });
                cmd.Parameters.Add(new OracleParameter("fundid", OracleDbType.Varchar2) { Value = fundId });

                conn.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
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