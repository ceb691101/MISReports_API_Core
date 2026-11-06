using MISReports_Api.Compat;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/jobcard")]
    public class JobcardController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly JobCardRepository _repository = new JobCardRepository();

        // GET api/jobcard?projectNo=XXX&costCtr=YYY
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetJobCards([FromUri] string projectNo, [FromUri] string costCtr)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("Cost center is required");

                if (string.IsNullOrWhiteSpace(projectNo))
                    return BadRequest("Project number is required");

                // Log input parameters
                System.Diagnostics.Trace.WriteLine($"Request received: costCtr={costCtr}, projectNo={projectNo}");

                // Call repository with correct parameter order
                var data = await _repository.GetJobCardsAsync(projectNo, costCtr);

                // Return success response
                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data = data
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetJobCards failed. projectNo={projectNo}, costCtr={costCtr}", projectNo, costCtr);
                // Log error details
                System.Diagnostics.Trace.WriteLine($"ERROR: {ex.ToString()}");

                // Return error response
                return Ok(new
                {
                    success = false,
                    message = "Error retrieving jobcard data",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}