using MISReports_Api.Models.BillMap;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.BillMap
{
    public class BillMapRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<BillMapModel>> GetBillMapsAsync(string epfNo)
        {
            var result = new List<BillMapModel>();

            string sql = @"
        SELECT
            b.BILL_MAP,
            b.COMP_NM,
            b.LEVEL_NO
        FROM REP_ROLE_NEW r
        JOIN REP_BILL_MAP b
            ON TRIM(b.COMP_ID) = TRIM(r.COMPANY)
        WHERE r.EPF_NO = :epf_no";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;

                cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = epfNo.Trim();

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new BillMapModel
                        {
                            BillMap = reader["BILL_MAP"]?.ToString(),
                            CompanyName = reader["COMP_NM"]?.ToString(),
                            LevelNo = reader["LEVEL_NO"]?.ToString()
                        });
                    }
                }
            }

            return result;
        }
    }
}