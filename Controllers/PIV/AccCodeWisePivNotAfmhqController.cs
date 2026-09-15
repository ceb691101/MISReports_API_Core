using MISReports_Api.DAL.PIV;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/acc-code-wise-piv-not-afmhq")]
    public class AccCodeWisePivNotAfmhqController : ApiController
    {
        private readonly AccCodeWisePivNotAfmhqRepository _repo = new AccCodeWisePivNotAfmhqRepository();

        // GET: api/acc-code-wise-piv-not-afmhq/report?fromDate=2025/01/01&toDate=2025/12/31&costctr=AFMHQ001
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string costctr)
        {
            if (string.IsNullOrWhiteSpace(fromDate) ||
                string.IsNullOrWhiteSpace(toDate) ||
                string.IsNullOrWhiteSpace(costctr))
            {
                return BadRequest("Parameters 'fromDate', 'toDate' and 'costctr' are required (fromDate/toDate format: yyyy/MM/dd)");
            }

            if (!DateTime.TryParseExact(fromDate, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.None, out DateTime dtFrom) ||
                !DateTime.TryParseExact(toDate, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.None, out DateTime dtTo))
            {
                return BadRequest("Invalid date format. Use yyyy/MM/dd");
            }

            if (dtFrom > dtTo)
            {
                return BadRequest("fromDate cannot be later than toDate");
            }

            try
            {
                var data = _repo.GetAccCodeWisePivNotAfmhq(dtFrom, dtTo, costctr.Trim());
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error generating PIV Non-AFMHQ Companies Report: " + ex.Message));
            }
        }
    }
}