using MISReports_Api.Compat;
using MISReports_Api.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/userrole")]
    public class UserRoleController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly UserRoleRepository _repository = new UserRoleRepository();

        [HttpGet]
        [Route("{epfNo}")]
        public IActionResult GetUserRole(string epfNo)
        {
            try
            {
                var result = _repository.GetUserRole(epfNo.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetUserRole failed. epfNo={epfNo}", epfNo);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get User Role.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
