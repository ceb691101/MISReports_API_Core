using MISReports_Api.Models;
using MISReports_Api.Repositories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provincepivprovincialpos")]
    public class ProvincePIVprovincialPOSController : ApiController
    {
        private readonly ProvincePIVprovincialRepository _repository =
            new ProvincePIVprovincialRepository();

        /// <summary>
        /// Sample:
        /// GET api/provincepivprovincialpos/get?compId=001&fromDate=20250101&toDate=20251231
        /// </summary>
        [HttpGet]
        [Route("get")]
        public IHttpActionResult Get(string compId, string fromDate, string toDate)
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
                return InternalServerError(new Exception(
                    "Error occurred while retrieving Province POS wise PIV Collections data.",
                    ex
                ));
            }
        }
    }
}