using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class LastDocRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<LastDocModel>> GetLastDocAsync(string deptId, int repYear)
        {
            var result = new List<LastDocModel>();

            string sql = @"
                SELECT 
                    DOC_PF,
                    MAX(DOC_NO) AS MAX_NO,
                    MAX(TRX_DT) AS MAX_TRX_DT,
                    (SELECT DEPT_NM 
                     FROM GLDEPTM 
                     WHERE DEPT_ID = :dept_id) AS CCT_NAME
                FROM INPOSTMT
                WHERE DEPT_ID = :dept_id
                  AND YR_IND = :rep_year
                GROUP BY DOC_PF
                ORDER BY DOC_PF";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;

                cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = deptId.Trim();
                cmd.Parameters.Add("rep_year", OracleDbType.Varchar2).Value = repYear.ToString();

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new LastDocModel
                        {
                            DocPrefix = reader["DOC_PF"]?.ToString(),
                            MaxDocNo = reader["MAX_NO"]?.ToString(),
                            MaxTrxDate = reader["MAX_TRX_DT"] != DBNull.Value
                                ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("MAX_TRX_DT"))
                                : null,
                            CostCenterName = reader["CCT_NAME"]?.ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}