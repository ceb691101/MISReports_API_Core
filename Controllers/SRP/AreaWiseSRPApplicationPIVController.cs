using MISReports_Api.DAL.SRP;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/areawisesrpapplicationpiv")]
    public class AreaWiseSRPApplicationPIVController : ApiController
    {
        private readonly AreaWiseSRPApplicationPIVRepository _repo = new AreaWiseSRPApplicationPIVRepository();

        // GET: api/areawisesrpapplicationpiv/get?compId=01&fromDate=20250101&toDate=20250131
        [HttpGet]
        [Route("get")]
        public IHttpActionResult Get(string compId, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(compId))
            {
                return BadRequest("compId is required.");
            }

            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
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
                var data = _repo.GetAreaWiseSRPApplicationPIVReport(compId, fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error while fetching Area Wise SRP Application PIV report: " + ex.Message));
            }
        }
    }
}
