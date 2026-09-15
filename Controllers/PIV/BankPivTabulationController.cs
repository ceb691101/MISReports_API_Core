using MISReports_Api.DAL.PIV;
using MISReports_Api.Models.PIV;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/bank-piv-tabulation")]
    public class BankPivTabulationController : ApiController
    {
        private readonly BankPivTabulationRepository _repo = new BankPivTabulationRepository();

        // GET: api/bank-piv-tabulation/report?fromDate=2025/01/01&toDate=2025/12/31
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetReport(string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("Parameters 'fromDate' and 'toDate' are required (format: yyyyMMdd)");
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
                var data = _repo.GetBankPivTabulation(dtFrom, dtTo);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error generating Bank PIV Tabulation Report: " + ex.Message));
            }
        }
    }
}