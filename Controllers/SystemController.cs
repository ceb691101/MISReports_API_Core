using MISReports_Api.Compat;
using System;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/user")]
    public class UserController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Connection string configured in Web.config
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["OracleTest"].ConnectionString;

        /// <summary>
        /// Fetches employee profile details by EPF Number or Role ID.
        /// </summary>
        [HttpGet]
        [Route("get-employee/{epfNo}")]
        public IActionResult GetEmployeeByEpf(string epfNo)
        {
            if (string.IsNullOrWhiteSpace(epfNo))
                return BadRequest("EPF number is required.");

            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    // Query REP_ROLE_NEW table matching EPF_NO or ROLEID while handling leading zeros and whitespace
                    string query = @"SELECT EPF_NO, ROLEID, ROLENAME, USERTYPE, COMPANY 
                                    FROM REP_ROLE_NEW 
                                    WHERE TRIM(EPF_NO) = TRIM(:epfNo) 
                                       OR LTRIM(TRIM(EPF_NO), '0') = LTRIM(TRIM(:epfNo), '0')
                                       OR TRIM(ROLEID) = TRIM(:epfNo)";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("epfNo", epfNo.Trim()));

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Format payload output for client application
                                var employeeData = new
                                {
                                    success = true,
                                    epfNo = reader["EPF_NO"] != DBNull.Value ? reader["EPF_NO"].ToString().Trim() : "",
                                    roleId = reader["ROLEID"] != DBNull.Value ? reader["ROLEID"].ToString().Trim() : "",
                                    name = reader["ROLENAME"] != DBNull.Value ? reader["ROLENAME"].ToString().Trim() : "",
                                    userType = reader["USERTYPE"] != DBNull.Value ? reader["USERTYPE"].ToString().Trim() : "",
                                    company = reader["COMPANY"] != DBNull.Value ? reader["COMPANY"].ToString().Trim() : ""
                                };

                                return Ok(employeeData);
                            }
                        }
                    }

                    return Ok(new { success = false, message = "Employee record not found in REP_ROLE_NEW." });
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetEmployeeByEpf failed. epfNo={epfNo}", epfNo);
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Fetches allocated systems and corresponding URLs assigned to the employee.
        /// </summary>
        [HttpGet]
        [Route("get-allocated-systems/{epfNo}")]
        public IActionResult GetAllocatedSystems(string epfNo)
        {
            if (string.IsNullOrWhiteSpace(epfNo))
                return BadRequest("EPF number is required.");

            try
            {
                using (OracleConnection conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    // Join ROLE_SYSTEM with SYSTEM_URL table to return system codes alongside target page URLs
                    string query = @"SELECT DISTINCT 
                                        TRIM(rs.SYSTEM) AS SYSTEM_CODE, 
                                        TRIM(su.URL) AS SYSTEM_URL 
                                    FROM ROLE_SYSTEM rs
                                    LEFT JOIN SYSTEM_URL su ON TRIM(rs.SYSTEM) = TRIM(su.SYSTEM)
                                    WHERE TRIM(rs.EPF_NO) = TRIM(:epfNo) 
                                       OR LTRIM(TRIM(rs.EPF_NO), '0') = LTRIM(TRIM(:epfNo), '0')";

                    using (OracleCommand cmd = new OracleCommand(query, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("epfNo", epfNo.Trim()));

                        using (OracleDataReader reader = cmd.ExecuteReader())
                        {
                            var systems = new List<object>();
                            while (reader.Read())
                            {
                                if (reader["SYSTEM_CODE"] != DBNull.Value)
                                {
                                    systems.Add(new
                                    {
                                        code = reader["SYSTEM_CODE"].ToString().Trim(),
                                        url = reader["SYSTEM_URL"] != DBNull.Value ? reader["SYSTEM_URL"].ToString().Trim() : ""
                                    });
                                }
                            }
                            return Ok(new { success = true, systems = systems });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAllocatedSystems failed. epfNo={epfNo}", epfNo);
                return InternalServerError(ex);
            }
        }
    }
}