using MISReports_Api.Compat;
using MISReports_Api.Models;
using MISReports_Api.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provincepivprovincialpos")]
    public class ProvincePIVprovincialPOSController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ProvincePIVprovincialRepository _repository =
            new ProvincePIVprovincialRepository();

        /// <summary>
        /// Sample:
        /// GET api/provincepivprovincialpos/get?compId=001&fromDate=20250101&toDate=20251231
        /// </summary>
        [HttpGet]
        [Route("get")]
        public IActionResult Get(string compId, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(compId) ||
                string.IsNullOrWhiteSpace(fromDate) ||
                string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("Parameters compId, fromDate, and toDate are required.");
            }

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fromDt) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime toDt))
            {
                return BadRequest("Dates must be in format yyyyMMdd (example: 20250101)");
            }

            try
            {
                var reportData = _repository.GetReport(compId, fromDt, toDt);
                return Ok(reportData);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Get failed. compId={compId}, fromDate={fromDate}, toDate={toDate}", compId, fromDate, toDate);
                return InternalServerError(new Exception(
                    "Error occurred while retrieving Province POS wise PIV Collections data.",
                    ex
                ));
            }
        }
    }
}