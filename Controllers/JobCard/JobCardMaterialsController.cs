using MISReports_Api.DAL;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/jobcardmaterials")]
    public class JobCardMaterialsController : ApiController
    {
        private readonly JobCardMaterialsRepository _repository = new JobCardMaterialsRepository();

        // GET api/jobcardmaterials?projectNo=XXX&costCtr=YYY
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetMaterials([FromUri] string projectNo, [FromUri] string costCtr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("Cost center is required");

                if (string.IsNullOrWhiteSpace(projectNo))
                    return BadRequest("Project number is required");

                System.Diagnostics.Trace.WriteLine($"JobCardMaterials Request: costCtr={costCtr}, projectNo={projectNo}");

                var data = await _repository.GetMaterialConsumptionAsync(projectNo, costCtr);

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data = data
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in JobCardMaterialsController: {ex}");
                return Ok(new
                {
                    success = false,
                    message = "Error retrieving material consumption data",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}