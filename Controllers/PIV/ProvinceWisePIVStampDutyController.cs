// File: ProvinceWisePIVStampDutyController.cs
// Location: MISReports_Api.Controllers

using MISReports_Api.Compat;
using MISReports_Api.DAL.PIV;
using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provincepivstampduty")]
    public class ProvinceWisePIVStampDutyController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ProvinceWisePIVStampDutyRepository _repo = new ProvinceWisePIVStampDutyRepository();

        // GET: api/provincepivstampduty/get?compID=1&fromDate=20250401&toDate=20250430
        [HttpGet]
        [Route("get")]
        public IActionResult Get(string compID, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(compID))
                return BadRequest("compID is required");

            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
                return BadRequest("fromDate and toDate are required.");

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
                return BadRequest("Invalid date format. Use yyyyMMdd (e.g. 20250401)");

            if (fromDt > toDt)
                return BadRequest("fromDate cannot be greater than toDate.");

            try
            {
                var data = _repo.GetProvinceWisePIVStampDutyReport(compID, fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Get failed. compID={compID}, fromDate={fromDate}, toDate={toDate}", compID, fromDate, toDate);
                return InternalServerError(new Exception("Error while fetching Province Wise PIV Stamp Duty report: " + ex.Message));
            }
        }
    }
}