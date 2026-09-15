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
    public class ConstructionAllDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        private static decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            OracleDecimal od = reader.GetOracleDecimal(ordinal);
            try
            {
                od = OracleDecimal.SetPrecision(od, 28);
                return od.Value;
            }
            catch (OverflowException)
            {
                return (decimal)(double)od;
            }
        }

        public List<ConstructionAllModel> GetConstructionAll(string costCtr)
        {
            var result = new List<ConstructionAllModel>();

            const string query = @"
                SELECT DISTINCT
                        (A.file_no || '-' || C.sestimate_no) AS file_no,
                        A.project_no,
                        TRIM(A.fund_id) AS fund_id,
                        A.con_by,
                        A.sup_by,
                        B.cpercentage AS c8,
                        B.enter_date,
                        C.file_ref,
                        D.std_cost,
                        A.code_number,
                        (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS cct_name
                FROM      pcmiledates B, pcinitialdata A, estimate_referencebs C, pcesthmt D
                WHERE     A.dept_id = B.dept_id
                  AND     TRIM(A.file_no) = TRIM(B.project_no)
                  AND     TRIM(A.file_no) = C.westimate_no
                  AND     TRIM(A.dept_id) = TRIM(:costctr)
                  AND     B.cpercentage >= 0
                  AND     B.cpercentage <= 100
                  AND     TRIM(D.estimate_no) = TRIM(C.westimate_no)
                  AND     D.dept_id = C.dept_id
                GROUP BY A.file_no, C.sestimate_no, TRIM(A.fund_id), A.con_by, A.project_no, A.sup_by,
                         A.decrp, B.cpercentage, B.project_no, B.mile_id, C.file_ref, B.enter_date,
                         C.westimate_no, C.dept_id, D.std_cost, A.code_number

                UNION ALL

                SELECT DISTINCT
                        (A.file_no || '-' || C.sestimate_no) AS file_no,
                        A.project_no,
                        TRIM(A.fund_id) AS fund_id,
                        A.con_by,
                        A.sup_by,
                        B.cpercentage AS c8,
                        B.enter_date,
                        C.file_ref,
                        D.std_cost,
                        A.code_number,
                        (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS cct_name
                FROM      pcmiledates B, pcinitialdata A, estimate_referencebs C, pcesthtt D
                WHERE     A.dept_id = B.dept_id
                  AND     TRIM(A.file_no) = TRIM(B.project_no)
                  AND     TRIM(A.file_no) = C.westimate_no
                  AND     TRIM(A.dept_id) = TRIM(:costctr)
                  AND     B.cpercentage >= 0
                  AND     B.cpercentage <= 100
                  AND     TRIM(D.estimate_no) = TRIM(C.westimate_no)
                  AND     D.dept_id = C.dept_id
                  AND     NOT EXISTS (SELECT 1
                                        FROM pcesthmt
                                       WHERE TRIM(estimate_no) = TRIM(D.estimate_no)
                                         AND D.dept_id = C.dept_id)
                GROUP BY A.file_no, C.sestimate_no, TRIM(A.fund_id), A.con_by, A.project_no, A.sup_by,
                         B.cpercentage, B.project_no, B.mile_id, C.file_ref, B.enter_date,
                         C.westimate_no, C.dept_id, D.std_cost, A.code_number

                ORDER BY 1, 6, 7";

            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used four times) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ConstructionAllModel
                        {
                            FileNo = reader["file_no"] == DBNull.Value ? null : reader["file_no"].ToString(),
                            ProjectNo = reader["project_no"] == DBNull.Value ? null : reader["project_no"].ToString(),
                            FundId = reader["fund_id"] == DBNull.Value ? null : reader["fund_id"].ToString(),
                            ConBy = reader["con_by"] == DBNull.Value ? null : reader["con_by"].ToString(),
                            SupBy = reader["sup_by"] == DBNull.Value ? null : reader["sup_by"].ToString(),
                            Cpercentage = SafeGetDecimal(reader, "c8"),
                            EnterDate = reader["enter_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["enter_date"]),
                            FileRef = reader["file_ref"] == DBNull.Value ? null : reader["file_ref"].ToString(),
                            StdCost = SafeGetDecimal(reader, "std_cost"),
                            CodeNumber = reader["code_number"] == DBNull.Value ? null : reader["code_number"].ToString(),
                            CctName = reader["cct_name"] == DBNull.Value ? null : reader["cct_name"].ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}