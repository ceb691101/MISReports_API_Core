using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/physical-verification-validation")]
    public class PHVValidationController : ApiController
    {
        private readonly PHVValidationRepository _repository;

        public PHVValidationController()
        {
            _repository = new PHVValidationRepository();
        }

        // GET api/physical-verification-validation?deptId=514.10&repYear=2022&repMonth=11
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetPHVValidationData(
            [FromUri] string deptId,
            [FromUri] string repYear,
            [FromUri] string repMonth)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId))
                    return BadRequest("deptId is required.");

                if (string.IsNullOrWhiteSpace(repYear))
                    return BadRequest("repYear is required.");

                if (string.IsNullOrWhiteSpace(repMonth))
                    return BadRequest("repMonth is required.");

                System.Diagnostics.Trace.WriteLine(
                    $"PHVValidation Request: deptId={deptId}, repYear={repYear}, repMonth={repMonth}"
                );

                var result = await _repository.GetPHVValidationDataAsync(
                    deptId.Trim(),
                    repYear.Trim(),
                    repMonth.Trim()
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
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR in PHVValidationController: {ex}"
                );

                return Ok(new
                {
                    success = false,
                    message = "Error retrieving physical validation data",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // GET api/physical-verification-validation/pdf?deptId=514.10&deptName=Stores&repYear=2022&repMonth=11&download=false
        [HttpGet]
        [Route("pdf")]
        public HttpResponseMessage GetPHVValidationPdf(
            [FromUri] string deptId,
            [FromUri] string deptName,
            [FromUri] string repYear,
            [FromUri] string repMonth,
            [FromUri] bool download = false)
        {
            return Request.CreateResponse(HttpStatusCode.NotImplemented, new
            {
                success = false,
                message = "JasperSoft reporting has been decommissioned. Please consume the primary endpoint for validation data."
            });
        }
    }
}
