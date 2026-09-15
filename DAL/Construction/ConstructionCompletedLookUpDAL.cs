using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.DAL
{
    public class ConstructionCompletedLookupDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<string> GetFundIds()
        {
            var result = new List<string>();

            const string query = @"
                SELECT DISTINCT fund_id
                FROM pcesthmt
                WHERE fund_id IS NOT NULL AND fund_id <> 'null'
                ORDER BY fund_id";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["fund_id"] != DBNull.Value)
                            result.Add(reader["fund_id"].ToString());
                    }
                }
            }

            return result;
        }

        public List<string> GetDistricts(string roleId)
        {
            var result = new List<string>();

            const string query = @"
                SELECT DISTINCT p.code_name
                FROM province_detail_master p
                WHERE p.code_name IS NOT NULL
                AND p.code_type = 'DISTRICT'
                AND EXISTS (
                        SELECT 1 
                        FROM rep_roles_cct_new r
                        WHERE r.roleid = :roleid
                        AND TO_CHAR(TO_NUMBER(p.dept_id), '99999.99') = 
                            TO_CHAR(TO_NUMBER(r.costcentre), '99999.99')
                        )
                ORDER BY p.code_name";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("roleid", OracleDbType.Varchar2) { Value = roleId });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader["code_name"] != DBNull.Value)
                            result.Add(reader["code_name"].ToString());
                    }
                }
            }

            return result;
        }
    }
}