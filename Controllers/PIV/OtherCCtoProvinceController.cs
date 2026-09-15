//05.Branch wise PIV Collections by Other Cost Centers relevant to the Province

using MISReports_Api.Repositories;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/othercctoprovince")]
    public class OtherCCtoProvinceController : ApiController
    {
        private readonly OtherCCtoProvinceRepository _repo = new OtherCCtoProvinceRepository();

        /// <summary>
        /// GET: api/othercctoprovince/get?compId=001&fromDate=20250401&toDate=20250430
        /// Report: Payments received by Province/Company departments from Other Cost Centers (outside own company hierarchy)
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
                var data = _repo.GetOtherCCtoProvinceReport(compId.Trim(), fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error while fetching Other CC to Province report: " + ex.Message));
            }
        }
    }
}