using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/costcenterworkinprogress")]
    public class CostCenterWorkInProgressController : ApiController
    {
        private readonly CostCenterWorkInProgressRepository _repository = new CostCenterWorkInProgressRepository();

        [HttpGet]
        [Route("{deptId}")]
        public async Task<IHttpActionResult> GetCostCenterWorkInProgress(string deptId)
        {
            Debug.WriteLine($"API Request received for deptId: {deptId}");

            try
            {
                var result = await _repository.GetCostCenterWorkInProgress(deptId.Trim());
                Debug.WriteLine($"Data retrieval completed. Records found: {result?.Count ?? 0}");

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                // More detailed error logging
                Debug.WriteLine($"Error in API: {ex.ToString()}");

                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Cost Center Work In Progress data.",
                    errorDetails = ex.Message,
                    stackTrace = ex.StackTrace
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        
    }
}