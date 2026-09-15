using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/annual-verification-sheet")]
    public class AnnualVerificationSheetController : ApiController
    {
        private readonly AnnualVerificationSheetRepository _repository;

        public AnnualVerificationSheetController()
        {
            _repository = new AnnualVerificationSheetRepository();
        }

        // GET api/annual-verification-sheet?deptId=510.11&repYear=2025&repMonth=11
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetAnnualVerificationSheet(
            [FromUri] string deptId,
            [FromUri] int repYear,
            [FromUri] int repMonth)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId))
                    return BadRequest("deptId is required.");

                if (repYear <= 0)
                    return BadRequest("repYear must be valid.");

                if (repMonth < 1 || repMonth > 12)
                    return BadRequest("repMonth must be between 1 and 12.");

                var data = await _repository.GetAnnualVerificationSheetAsync(
                    deptId.Trim(),
                    repYear,
                    repMonth
                );

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = "Error retrieving annual verification sheet",
                    detailedError = ex.Message
                });
            }
        }
    }
}
