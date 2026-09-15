//Area-wise Solar Sent to Billing Details

using MISReports_Api.DAL;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/solarbilling")]
    public class SolarBillingController : ApiController
    {
        private readonly SolarBillingRepository _repo = new SolarBillingRepository();

        // GET: api/solarbilling/get?compId=123&fromDate=20250101&toDate=20251231
        [HttpGet]
        [Route("get")]
        public IHttpActionResult Get(string compId, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(compId))
            {
                return BadRequest("compId is required.");
            }

            if (string.IsNullOrWhiteSpace(compId) || string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("fromDate and toDate are required.");
            }

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
            {
                return BadRequest("Invalid date format. Use yyyyMMdd (e.g. 20250101)");
            }

            if (fromDt > toDt)
            {
                return BadRequest("fromDate cannot be greater than toDate.");
            }

            try
            {
                var data = _repo.GetSolarBillingReport(compId, fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error while fetching Solar Billing report: " + ex.Message));
            }
        }
    }
}