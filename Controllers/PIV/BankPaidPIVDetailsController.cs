using MISReports_Api.DAL.PIV;
using MISReports_Api.Models.PIV;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/bank-paid-piv")]
    public class BankPaidPIVDetailsController : ApiController
    {
        private readonly BankPaidPIVDetailsRepository _repo = new BankPaidPIVDetailsRepository();

        // GET: api/bank-paid-piv/report?fromDate=2025/01/01&toDate=2025/12/31
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetReport(string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("Parameters 'fromDate' and 'toDate' are required (format: yyyy/MM/dd)");
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
                var data = _repo.GetBankPaidPIVDetails(dtFrom, dtTo);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error generating Bank Paid PIV Report: " + ex.Message));
            }
        }
    }
}