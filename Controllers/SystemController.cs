using System;
using System.Configuration;
using System.Data;
using System.Web.Http;
using Oracle.ManagedDataAccess.Client;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/user")]
    public class UserController : ApiController
    {
        // Retrieve connection string from Web.config
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["OracleTest"].ConnectionString;

        [HttpGet]
        [Route("get-employee/{epfNo}")]
        public IHttpActionResult GetEmployeeByEpf(string epfNo)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    // Query REP_ROLE_NEW table to match by EPF_NO or ROLEID
                    string query = @"SELECT EPF_NO, ROLEID, ROLENAME, USERTYPE, COMPANY 
                                    FROM REP_ROLE_NEW 
                                    WHERE EPF_NO = :epfNo OR ROLEID = :epfNo";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        // Bind endpoint parameter to SQL parameter
                        cmd.Parameters.Add(new OracleParameter("epfNo", epfNo));

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Map Oracle record fields into JSON payload
                                var employeeData = new
                                {
                                    success = true,
                                    epfNo = reader["EPF_NO"] != DBNull.Value ? reader["EPF_NO"].ToString().Trim() : "",
                                    roleId = reader["ROLEID"] != DBNull.Value ? reader["ROLEID"].ToString().Trim() : "",
                                    name = reader["ROLENAME"] != DBNull.Value ? reader["ROLENAME"].ToString().Trim() : "", // Trims trailing spaces
                                    userType = reader["USERTYPE"] != DBNull.Value ? reader["USERTYPE"].ToString().Trim() : "",
                                    company = reader["COMPANY"] != DBNull.Value ? reader["COMPANY"].ToString().Trim() : ""
                                };

                                return Ok(employeeData);
                            }
                        }
                    }
                }

                return Ok(new { success = false, message = "Employee record not found in REP_ROLE_NEW." });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}