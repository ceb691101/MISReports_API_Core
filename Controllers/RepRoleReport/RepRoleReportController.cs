using MISReports_Api.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/reprolereports")]
    public class RepRoleReportController : ApiController
    {
        private readonly RepRoleReportRepository _repository =
            new RepRoleReportRepository();

        // GET api/reprolereports/get?roleId=niro
        [HttpGet]
        [Route("get")]
        public async Task<IHttpActionResult> GetReports(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
                return BadRequest("roleId is required.");

            try
            {
                var result = await _repository.GetReportsByRole(roleId);

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot fetch report list.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}