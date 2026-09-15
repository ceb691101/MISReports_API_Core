using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class UserRoleRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<UserRoleModel> GetUserRole(string epfNo)
        {
            var roles = new List<UserRoleModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT UPPER(r.roleid) AS RoleId, r.USERTYPE, r.COMPANY, r.USER_GROUP,
                                          NVL(b.bill_map, 'NA') AS bill_map,
                                          NVL(b.level_no, 0) AS level_no
                                   FROM rep_role_new r
                                   LEFT JOIN REP_BILL_MAP b ON trim(r.company) = trim(b.comp_id)
                                   WHERE r.epf_no = :epf_no";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("epf_no", epfNo);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                roles.Add(new UserRoleModel
                                {
                                    RoleId = reader["RoleId"]?.ToString(),
                                    USERTYPE = reader["USERTYPE"]?.ToString(),
                                    COMPANY = reader["COMPANY"]?.ToString(),
                                    UserGroup = reader["USER_GROUP"]?.ToString(),
                                    BillMap = reader["bill_map"] == DBNull.Value ? "NA" : reader["bill_map"]?.ToString() ?? "NA",
                                    LevelNo = reader["level_no"] == DBNull.Value ? "0" : reader["level_no"]?.ToString() ?? "0"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetUserRole: {ex.Message}");
                throw;
            }

            return roles;
        }
    }
}