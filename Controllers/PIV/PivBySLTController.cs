//07.2 PIV Collections by IPG ( SLT )

// File: Controllers/PivBySLTController.cs
using MISReports_Api.DAL.PIV;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/pivbyslt")]
    public class PivBySLTController : ApiController
    {
        private readonly PivBySLTRepository _repo = new PivBySLTRepository();

        // GET: api/pivbysltbank/get?fromDate=20250401&toDate=20250430
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
                var data = _repo.GetPivBySLTReport(fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error while fetching PIV by People (Bank) report: " + ex.Message));
            }
        }
    }
}