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
    [RoutePrefix("api/projectcommitmentsummary")]
    public class ProjectCommitmentSummaryController : ApiController
    {
        private readonly ProjectCommitmentSummaryRepository _repository = new ProjectCommitmentSummaryRepository();

        [HttpGet]
        [Route("{deptId}")]
        public async Task<IHttpActionResult> GetProjectCommitmentSummary(string deptId)
        {
            try
            {
                var result = await _repository.GetProjectCommitmentSummary(deptId.Trim());

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
                    errorMessage = "Cannot get Project Commitment Summary data.",
                    errorDetails = ex.Message,
                    stackTrace = ex.StackTrace
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
