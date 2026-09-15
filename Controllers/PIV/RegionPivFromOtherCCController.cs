//23.Region wise PIV Collections by Provincial POS relevant to Other Cost Centers
using MISReports_Api.Repositories;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/Regional-POS-relevant-to-OtherCC")]
    public class RegionPivFromOtherCCController : ApiController
    {
        private readonly RegionPivFromOtherCCRepository _repo = new RegionPivFromOtherCCRepository();

        /// <summary>
        /// GET: api/Regional-POS-relevant-to-OtherCC/get?compId=001&fromDate=20250401&toDate=20250430
        /// Report: PIV payments received by Region/Company departments
        /// from Other Cost Centers (outside own company hierarchy)
        /// </summary>
        [HttpGet]
        [Route("get")]
        public IHttpActionResult Get(string compId, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(compId) ||
                string.IsNullOrWhiteSpace(fromDate) ||
                string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("compId, fromDate and toDate are required.");
            }

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
            {
                return BadRequest("Invalid date format. Use yyyyMMdd (e.g. 20250401)");
            }

            try
            {
                var data = _repo.GetRegionPivFromOtherCC(compId.Trim(), fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching Region PIV from Other CC report: " + ex.Message));
            }
        }
    }
}