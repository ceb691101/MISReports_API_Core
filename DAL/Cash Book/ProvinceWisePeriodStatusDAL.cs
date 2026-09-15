// DAL/ProvinceWisePeriodStatusDAL.cs
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class ProvinceWisePeriodStatusDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvinceWisePeriodStatusModel> GetProvinceWisePeriodStatus(int repYear, int repMonth, string compId)
        {
            var result = new List<ProvinceWisePeriodStatusModel>();

            const string query = @"
            SELECT
                TRIM(T1.DEPT_ID) AS DEPT_ID,
                TRIM(T2.DEPT_NM) AS DEPT_NM,
                TRIM(T2.COMP_ID) AS COMP_ID,
                T1.FIN_YEAR,
                T1.FIN_PRD,
                (CASE WHEN T1.PRD_STAT = 1 THEN 'Future period'
                      WHEN T1.PRD_STAT = 2 THEN 'Open period'
                      WHEN T1.PRD_STAT = 3 THEN 'Current period'
                      WHEN T1.PRD_STAT = 4 THEN 'Soft closed period'
                      WHEN T1.PRD_STAT = 5 THEN 'Hard closed period'
                      ELSE TO_CHAR(T1.PRD_STAT) || ' - Unknown' END) AS STATUS,
                (SELECT TRIM(COMP_NM) FROM GLCOMPM WHERE TRIM(COMP_ID) = :compid) AS COMP_NM
            FROM
                GLFNPRDM T1
                INNER JOIN GLDEPTM T2 ON TRIM(T1.DEPT_ID) = TRIM(T2.DEPT_ID)
            WHERE
                T1.FIN_YEAR = :repyear
                AND T1.FIN_PRD = :repmonth
                AND TRIM(T2.COMP_ID) IN (
                    SELECT TRIM(COMP_ID) FROM GLCOMPM 
                    WHERE TRIM(COMP_ID) = :compid OR TRIM(PARENT_ID) = :compid OR TRIM(GRP_COMP) = :compid
                )
            ORDER BY
                TRIM(T1.DEPT_ID)";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compId.Trim() });
                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Int32) { Value = repYear });
                cmd.Parameters.Add(new OracleParameter("repmonth", OracleDbType.Int32) { Value = repMonth });

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new ProvinceWisePeriodStatusModel
                            {
                                DeptId = SafeStr(reader, "DEPT_ID"),
                                DeptNm = SafeStr(reader, "DEPT_NM"),
                                CompId = SafeStr(reader, "COMP_ID"),
                                FinYear = SafeInt(reader, "FIN_YEAR"),
                                FinPrd = SafeInt(reader, "FIN_PRD"),
                                Status = SafeStr(reader, "STATUS"),
                                CompNm = SafeStr(reader, "COMP_NM")
                            });
                        }
                    }
                }
                catch (OracleException oex)
                {
                    throw new Exception($"Oracle error {oex.Number}: {oex.Message}", oex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching province-wise period status data from database", ex);
                }
            }

            return result;
        }
        #region Safe Readers

        private string SafeStr(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? null : r.GetString(ord);
        }

        private int? SafeInt(OracleDataReader r, string col)
        {
            int ord = r.GetOrdinal(col);
            return r.IsDBNull(ord) ? (int?)null : Convert.ToInt32(r.GetDecimal(ord));
        }

        #endregion
    }
}