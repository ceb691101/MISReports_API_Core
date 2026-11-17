// 06. Branch wise PIV Tabulation (Both Bank and POS) Report
using MISReports_Api.Compat;
using MISReports_Api.Repositories;
using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/branchwisepivboth")]
    public class BranchWisePivBothController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly BranchWisePivBothRepository _repo = new BranchWisePivBothRepository();

        // GET: api/branchwisepivboth/get?compId=001&fromDate=20250401&toDate=20250430
        [HttpGet]
        [Route("get")]
        public IActionResult Get(string compId, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(compId) ||
                string.IsNullOrWhiteSpace(fromDate) ||
                string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("compId, fromDate and toDate are required.");
            }

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
            {
                return BadRequest("Invalid date format. Use yyyyMMdd (e.g. 20250401)");
            }

            try
            {
                var data = _repo.GetBranchWisePivBothReport(compId.Trim(), fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Get failed. compId={compId}, fromDate={fromDate}, toDate={toDate}", compId, fromDate, toDate);
                return InternalServerError(new Exception("Error while fetching Branch wise PIV Both report: " + ex.Message));
            }
        }
    }
}