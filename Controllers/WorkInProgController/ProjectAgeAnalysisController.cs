using MISReports_Api.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/projectageanalysis")]
    public class ProjectAgeAnalysisController : ApiController
    {
        private readonly ProjectAgeAnalysisRepository _repository = new ProjectAgeAnalysisRepository();

        [HttpGet]
        [Route("{deptId}")]
        public async Task<IHttpActionResult> GetProjectAgeAnalysis(string deptId)
        {
            Debug.WriteLine($"API Request received for ProjectAgeAnalysis deptId: {deptId}");

            try
            {
                var result = await _repository.GetProjectAgeAnalysis(deptId.Trim());

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
                    errorMessage = "Cannot get Project Age Analysis data.",
                    errorDetails = ex.Message,
                    stackTrace = ex.StackTrace
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
