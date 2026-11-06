using MISReports_Api.Compat;
using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/annual-verification-sheet")]
    public class AnnualVerificationSheetController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AnnualVerificationSheetRepository _repository;

        public AnnualVerificationSheetController()
        {
            _repository = new AnnualVerificationSheetRepository();
        }

        // GET api/annual-verification-sheet?deptId=510.11&repYear=2025&repMonth=11
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetAnnualVerificationSheet(
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
                logger.Error(ex, "GetAnnualVerificationSheet failed. deptId={deptId}, repYear={repYear}, repMonth={repMonth}", deptId, repYear, repMonth);
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
