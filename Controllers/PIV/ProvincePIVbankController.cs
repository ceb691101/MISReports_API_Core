//01.Branch/Province wise PIV Collections Paid to Bank

using MISReports_Api.Repositories;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provincepivbank")]
    public class ProvincePIVbankController : ApiController
    {
        private readonly ProvincePIVbankRepository _repository = new ProvincePIVbankRepository();

        /// <summary>
        /// GET: api/provincepivbank/get?compId=001&fromDate=20250101&toDate=20251231
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
                var data = _repository.GetProvincePIVbankReport(compId, fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching Province PIV Bank report.", ex));
            }
        }
    }
}