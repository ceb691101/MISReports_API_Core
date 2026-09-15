using MISReports_Api.DAL;
using System;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercardtotal")]
    public class LedgerCardTotalController : ApiController
    {
        private readonly LedgerCardTotalRepository _repo = new LedgerCardTotalRepository();

        // ---------- MAIN ROUTE (used by front-end) ----------
        [HttpGet]
        [Route("report/{glcode}/{repyear:int}/{repmonth:int}")]
        public IHttpActionResult GetReport(string glcode, int repyear, int repmonth)
        {
            return GetLedgerCardTotalData(glcode, repyear, repmonth);
        }

        // ---------- LEGACY query-string support ----------
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get(
            [FromUri] string glcode,
            [FromUri] int repyear,
            [FromUri] int repmonth)
        {
            return GetLedgerCardTotalData(glcode, repyear, repmonth);
        }

        private IHttpActionResult GetLedgerCardTotalData(string glcode, int repyear, int repmonth)
        {
            try
            {
                // ---- Validation ----
                if (string.IsNullOrWhiteSpace(glcode))
                    return BadRequest("glcode is required.");

                if (repyear < 2000 || repyear > 2100)
                    return BadRequest("repyear must be between 2000 and 2100.");

                if (repmonth < 1 || repmonth > 12)
                    return BadRequest("repmonth must be between 1 and 12.");

                // ---- Data ----
                var data = _repo.GetLedgerCardTotalData(glcode.Trim(), repyear, repmonth);

                // ---- Totals ----
                decimal totalDebit = data.Sum(x => x.DrAmt ?? 0m);
                decimal totalCredit = data.Sum(x => x.CrAmt ?? 0m);

                // ---- Summary (first row holds GL-level info) ----
                var first = data.FirstOrDefault();

                return Ok(new
                {
                    success = true,
                    message = data.Any()
                        ? "Data retrieved successfully"
                        : "No records found for the given criteria",
                    data = data,
                    summary = new
                    {
                        glCode = glcode.Trim(),
                        accountName = first?.AcName,
                        costCenter = first?.CctName,
                        reportYear = repyear,
                        period = $"{repmonth:D2}",
                        periodDisplay = $"{GetMonthName(repmonth)} {repyear}",
                        glOpeningBalance = first?.GLOpeningBalance,
                        glClosingBalance = first?.GLClosingBalance,
                        totalRecords = data.Count,
                        totalDebit,
                        totalCredit,
                        netMovement = totalDebit - totalCredit
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error in LedgerCardTotalController: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(
                    new Exception($"Error fetching ledger card total data: {ex.Message}", ex));
            }
        }

        private string GetMonthName(int month)
            => new DateTime(2000, month, 1).ToString("MMMM");
    }
}