using MISReports_Api.DAL;
using System;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/cashbook")]
    public class CashBookReportController : ApiController
    {
        private readonly CashBookReportRepository _repo = new CashBookReportRepository();

        // MAIN: /api/cashbook/report/20220101/20251231/LTL
        [HttpGet]
        [Route("report/{fromDate:regex(^\\d{{8}}$)}/{toDate:regex(^\\d{{8}}$)}/{payee?}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string payee = null)
        {
            return GetData(fromDate, toDate, payee);
        }

        // LEGACY: ?fromDate=20220101&toDate=20251231&payee=LTL
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get(
            [FromUri] string fromDate,
            [FromUri] string toDate,
            [FromUri] string payee = null)
        {
            return GetData(fromDate, toDate, payee);
        }

        private IHttpActionResult GetData(string fromDate, string toDate, string payee)
        {
            try
            {
                if (!IsValidYyyyMmDd(fromDate) || !IsValidYyyyMmDd(toDate))
                    return BadRequest("Dates must be in YYYYMMDD format (e.g., 20220101).");

                payee = (payee ?? "").Trim();
                if (payee.Length == 0)
                    return BadRequest("Payee is required.");

                if (payee.Length < 3)
                    return BadRequest("Payee must be at least 3 characters for accurate search.");

                var data = _repo.GetCashBookData(fromDate, toDate, payee);
                decimal totalAmt = data.Sum(x => x.ChqAmt ?? 0m);

                const int MAX_RECORDS = 5000;
                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records found ({data.Count}). Result capped at {MAX_RECORDS}. Please use a more specific payee or narrower date range.",
                        data = new object[0],
                        summary = new
                        {
                            fromDate,
                            toDate,
                            payeeFilter = payee,
                            totalRecords = data.Count,
                            totalAmount = totalAmt,
                            warning = "RESULT_CAPPED"
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = data.Any() ? "Cash-book data retrieved successfully" : "No records found",
                    data,
                    summary = new
                    {
                        fromDate,
                        toDate,
                        payeeFilter = payee,
                        totalRecords = data.Count,
                        totalAmount = totalAmt
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in CashBookReportController: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Error fetching cash-book report: {ex.Message}", ex));
            }
        }

        private bool IsValidYyyyMmDd(string s)
        {
            if (s == null || s.Length != 8) return false;

            if (!int.TryParse(s.Substring(0, 4), out int y) || y < 1900 || y > 2100) return false;
            if (!int.TryParse(s.Substring(4, 2), out int m) || m < 1 || m > 12) return false;
            if (!int.TryParse(s.Substring(6, 2), out int d) || d < 1 || d > 31) return false;

            return true;
        }
    }
}