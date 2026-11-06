using MISReports_Api.Compat;
using MISReports_Api.DAL;
using NLog;
using System;
using System.Net;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/debtors")]
    public class GovernmentAgeAnalysisController : ApiController
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        [HttpGet]
        [Route("bulk/sectors")]
        public IActionResult GetSectors()
        {
            try
            {
                var repository = new GovernmentAgeAnalysisRepository();
                return Ok(new { data = repository.GetFilters(), errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Cannot load government bulk age analysis filters.");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    data = (object)null,
                    errorMessage = "Cannot load government report sectors and bill cycles. Check the bulk database connection."
                });
            }
        }

        [HttpGet]
        [Route("bulk/government-age-analysis/locations")]
        public IActionResult GetLocations([FromUri] string areaType = "Entire CEB")
        {
            areaType = areaType?.Trim();
            if (!IsValidAreaType(areaType))
                return BadRequest("Area type must be Province, Division, or Entire CEB.");
            try
            {
                var repository = new GovernmentAgeAnalysisRepository();
                return Ok(new { data = repository.GetLocations(areaType), errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Cannot load government bulk report locations.");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    data = (object)null,
                    errorMessage = "Cannot load provinces and divisions. Check the bulk database connection."
                });
            }
        }

        [HttpGet]
        [Route("bulk/government-age-analysis")]
        public IActionResult GetGovernmentAgeAnalysis(
            [FromUri] string billCycle = null,
            [FromUri] string reportType = "Detailed",
            [FromUri] string sectorCode = null,
            [FromUri] string areaType = "Entire CEB",
            [FromUri] string locationCode = null)
        {
            reportType = reportType?.Trim();
            if (reportType != null && reportType.EndsWith(" report", StringComparison.OrdinalIgnoreCase))
                reportType = reportType.Substring(0, reportType.Length - " report".Length).Trim();
            sectorCode = sectorCode?.Trim().ToUpperInvariant();
            areaType = areaType?.Trim();
            locationCode = locationCode?.Trim().ToUpperInvariant();
            if (!IsValidAreaType(areaType))
                return BadRequest("Area type must be Province, Division, or Entire CEB.");
            bool needsLocation = !areaType.Equals("Entire CEB", StringComparison.OrdinalIgnoreCase);
            if (needsLocation && string.IsNullOrWhiteSpace(locationCode))
                return BadRequest("Location code is required when Area Type is Province or Division.");
            int cycle;
            if (!int.TryParse(billCycle, out cycle) || cycle < 101)
                return BadRequest("A valid bill cycle is required.");
            if (!string.Equals(reportType, "Detailed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(reportType, "Full", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(reportType, "Summary", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Report type must be Detailed, Full, or Summary.");
            if (reportType.Equals("Detailed", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(sectorCode))
                return BadRequest("Sector code is required for a Detailed report.");

            try
            {
                var repository = new GovernmentAgeAnalysisRepository();
                if (needsLocation && !repository.GetLocations(areaType)
                    .Any(location => location.Code.Equals(locationCode, StringComparison.OrdinalIgnoreCase)))
                    return BadRequest($"Unknown {areaType} code '{locationCode}'. Use a Code from GET api/debtors/bulk/government-age-analysis/locations?areaType={areaType}.");
                if (reportType.Equals("Detailed", StringComparison.OrdinalIgnoreCase)
                    && !repository.SectorExists(sectorCode))
                    return BadRequest($"Unknown sector code '{sectorCode}'. Use a CatCode from GET api/debtors/bulk/sectors, for example A for Agriculture.");
                return Ok(new
                {
                    data = repository.GetReport(cycle.ToString(), reportType, sectorCode, areaType, locationCode),
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Cannot generate government bulk age analysis.");
                return Content(HttpStatusCode.InternalServerError, new
                {
                    data = (object)null,
                    errorMessage = "Cannot generate government age analysis. Check the bulk database connection."
                });
            }
        }

        // Preserve existing callers while Swagger documents the bulk routes.
        [HttpGet]
        [Route("sectors")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetLegacySectors() => GetSectors();

        [HttpGet]
        [Route("government-age-analysis/locations")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetLegacyLocations([FromUri] string areaType = "Entire CEB")
            => GetLocations(areaType);

        [HttpGet]
        [Route("government-age-analysis")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetLegacyGovernmentAgeAnalysis(
            [FromUri] string billCycle = null,
            [FromUri] string reportType = "Detailed",
            [FromUri] string sectorCode = null,
            [FromUri] string areaType = "Entire CEB",
            [FromUri] string locationCode = null)
            => GetGovernmentAgeAnalysis(billCycle, reportType, sectorCode, areaType, locationCode);

        private static bool IsValidAreaType(string areaType)
            => string.Equals(areaType, "Province", StringComparison.OrdinalIgnoreCase)
                || string.Equals(areaType, "Division", StringComparison.OrdinalIgnoreCase)
                || string.Equals(areaType, "Entire CEB", StringComparison.OrdinalIgnoreCase);
    }
}
