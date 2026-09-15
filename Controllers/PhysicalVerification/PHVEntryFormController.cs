using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using MISReports_Api.DAL.PhysicalVerification;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/physical-verification")]
    public class PHVEntryFormController : ApiController
    {
        private readonly PhysicalVerificationRepository _repository;

        public PHVEntryFormController()
        {
            _repository = new PhysicalVerificationRepository();
        }

        // GET api/physical-verification?deptId=520.11&docNo=520.11/PHV/22/0001
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetPhysicalVerification(
            [FromUri] string deptId,
            [FromUri] string docNo)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(deptId))
                    return BadRequest("deptId is required.");

                if (string.IsNullOrWhiteSpace(docNo))
                    return BadRequest("docNo is required.");

                // Log inputs
                System.Diagnostics.Trace.WriteLine(
                    $"PHVEntryForm Request: deptId={deptId}, docNo={docNo}"
                );

                // Call async repository
                var result = await _repository.GetPhysicalVerificationDataAsync(
                    deptId.Trim(),
                    docNo.Trim()
                );

                if (result == null || !result.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        count = 0,
                        data = result
                    });
                }

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
                });
            }
            catch (Exception ex)
            {
                // Error logging
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR in PHVEntryFormController: {ex}"
                );

                // Standardized error response
                return Ok(new
                {
                    success = false,
                    message = "Error retrieving physical verification data",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}
