using MISReports_Api.Compat;
using MISReports_Api.DAL.PIV;
using MISReports_Api.Models.PIV;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/stampduty")]
    public class StampDutyDetailedController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly StampDutyDetailedRepository _repo = new StampDutyDetailedRepository();

        // GET: api/stampduty/detailed?compId=COMP001&fromDate=2025/01/01&toDate=2025/12/31
        [HttpGet]
        [Route("detailed")]
        public IActionResult GetDetailedReport(string compId, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(compId))
                return BadRequest("Company ID (compId) is required");

            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
                return BadRequest("Both 'fromDate' and 'toDate' parameters are required (format: yyyyMMdd)");

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dtFrom) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime dtTo))
                return BadRequest("Invalid date format. Use yyyyMMdd");

            if (dtFrom > dtTo)
                return BadRequest("fromDate cannot be later than toDate");

            try
            {
                List<StampDutyDetailedModel> data = _repo.GetStampDutyDetailedReport(compId.Trim(), dtFrom, dtTo);
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetDetailedReport failed. compId={compId}, fromDate={fromDate}, toDate={toDate}", compId, fromDate, toDate);
                return InternalServerError(new Exception("Error generating Stamp Duty Detailed report: " + ex.Message));
            }
        }
    }
}