using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class RepRoleReportRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<RepRoleReportModel>> GetReportsByRole(string roleId)
        {
            var result = new List<RepRoleReportModel>();

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
SELECT 
    TRIM(rr.repid_no) AS repid_no,
    TRIM(rc.catname) AS catname,
    TRIM(rp.repname) AS repname
FROM REP_ROLES_REP_NEW rr
JOIN REP_CATS_NEW rc 
    ON TRIM(rr.catcode) = TRIM(rc.catcode)
JOIN REP_REPORTS_NEW rp 
    ON TRIM(rr.repid_no) = TRIM(rp.repid_no)
   AND TRIM(rr.repid) = TRIM(rp.repid)
   AND TRIM(rr.catcode) = TRIM(rp.catcode)
WHERE 
    TRIM(rr.roleid) = :roleId
    AND rr.favorite = 1
ORDER BY rc.catname, rp.repname";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;

                    cmd.Parameters.Add("roleId", OracleDbType.Varchar2)
                                   .Value = roleId.Trim(); // IMPORTANT

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new RepRoleReportModel
                            {
                                RepIdNo = reader["repid_no"]?.ToString(),
                                CategoryName = reader["catname"]?.ToString(),
                                ReportName = reader["repname"]?.ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}