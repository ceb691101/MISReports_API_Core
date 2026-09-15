// File: Controllers/ProvincePIVAllController.cs
using MISReports_Api.Repositories;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provincepivall")]
    public class ProvincePIVAllController : ApiController
    {
        private readonly ProvincePIVAllRepository _repository = new ProvincePIVAllRepository();

        /// <summary>
        /// GET: api/provincepivall/get?compId=001&fromDate=20250101&toDate=20251231
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

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
            {
                return BadRequest("Date format must be yyyyMMdd (e.g., 20250101)");
            }

            try
            {
                var data = _repository.GetProvincePIVAllReport(compId, fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching Province PIV All report.", ex));
            }
        }
    }
}