using MISReports_Api.DAL;
using System;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/divisionalledgercard")]
    public class DivisionalLedgerCardController : ApiController
    {
        private readonly DivisionalLedgerCardRepository _repo = new DivisionalLedgerCardRepository();

        // ---------- MAIN ROUTE ----------
        [HttpGet]
        [Route("report/{year:int}/{month:int}/{glcode}/{company}")]
        public IHttpActionResult GetReport(int year, int month, string glcode, string company)
        {
            return GetData(year, month, glcode, company);
        }

        // ---------- LEGACY Query String ----------
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get(
            [FromUri] int year,
            [FromUri] int month,
            [FromUri] string glcode,
            [FromUri] string company)
        {
            return GetData(year, month, glcode, company);
        }

        private IHttpActionResult GetData(int year, int month, string glcode, string company)
        {
            try
            {
                // ---- Validation ----
                if (year < 2000 || year > 2100)
                    return BadRequest("year must be between 2000 and 2100.");
                if (month < 1 || month > 12)
                    return BadRequest("month must be between 1 and 12.");
                if (string.IsNullOrWhiteSpace(glcode))
                    return BadRequest("glcode is required.");
                if (string.IsNullOrWhiteSpace(company))
                    return BadRequest("company is required.");

                // ---- Data ----
                var data = _repo.GetDivisionalLedgerCardData(year, month, glcode.Trim(), company.Trim());

                // ---- Totals ----
                decimal totalDebit = data.Sum(x => x.DrAmt ?? 0m);
                decimal totalCredit = data.Sum(x => x.CrAmt ?? 0m);

                // ---- First row for summary (optional) ----
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
                        reportYear = year,
                        reportMonth = month,
                        periodDisplay = $"{GetMonthName(month)} {year}",
                        glCodePart = glcode,
                        company,
                        totalRecords = data.Count,
                        totalDebit,
                        totalCredit,
                        netMovement = totalDebit - totalCredit,
                        openingBalance = first?.OpBal,
                        closingBalance = first?.ClBal
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Error in DivisionalLedgerCardController: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(
                    new Exception($"Error fetching divisional ledger card: {ex.Message}", ex));
            }
        }

        private string GetMonthName(int month)
            => new DateTime(2000, month, 1).ToString("MMMM");
    }
}