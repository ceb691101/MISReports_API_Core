//22. Refunded PIV Details
using MISReports_Api.Compat;
using MISReports_Api.DAL.PIV;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/refunded-piv")]
    public class RefundedPivController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly RefundedPivRepository _repo = new RefundedPivRepository();

        // GET: api/refunded-piv/report?fromDate=2025/01/01&toDate=2025/12/31&costctr=AFMHQ001
        [HttpGet]
        [Route("report")]
        public IActionResult GetReport(string fromDate, string toDate, string costctr)
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
                var data = _repo.GetRefundedPivs(dtFrom, dtTo, costctr.Trim());
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetReport failed. fromDate={fromDate}, toDate={toDate}, costctr={costctr}", fromDate, toDate, costctr);
                return InternalServerError(new Exception("Error generating Refunded PIV Report: " + ex.Message));
            }
        }
    }
}