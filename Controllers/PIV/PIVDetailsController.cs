using MISReports_Api.DAL.PIV;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/pivdetails")]
    public class PIVDetailsController : ApiController
    {
        private readonly PIVDetailsRepository _repo = new PIVDetailsRepository();

        // GET: api/pivdetails/get?fromDate=20250401&toDate=20250430
        [HttpGet]
        [Route("get")]
        public IHttpActionResult Get(string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("fromDate and toDate are required.");
            }

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
            {
                return BadRequest("Invalid date format. Use yyyyMMdd (e.g. 20250401)");
            }

            if (fromDt > toDt)
            {
                return BadRequest("fromDate cannot be greater than toDate.");
            }

            try
            {
                var data = _repo.GetPIVDetailsReport(fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error while fetching PIV Details report: " + ex.Message));
            }
        }
    }
}