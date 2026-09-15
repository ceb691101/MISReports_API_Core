using MISReports_Api.DAL;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercard")]
    public class LedgerCardController : ApiController
    {
        private readonly LedgerCardRepository _repository;

        public LedgerCardController()
        {
            _repository = new LedgerCardRepository();
        }

        // MAIN ROUTE USED BY FRONTEND
        [HttpGet]
        [Route("report/{glcode}/{repyear:int}/{startmonth:int}/{endmonth:int}")]
        public IHttpActionResult GetReport(string glcode, int repyear, int startmonth, int endmonth)
        {
            return GetLedgerCardData(glcode, repyear, startmonth, endmonth);
        }

        // LEGACY ROUTE (optional – for query string support)
        [HttpGet]
        [Route("")]
        public IHttpActionResult Get(
            [FromUri] string glcode,
            [FromUri] int repyear,
            [FromUri] int startmonth,
            [FromUri] int endmonth)
        {
            return GetLedgerCardData(glcode, repyear, startmonth, endmonth);
        }

        private IHttpActionResult GetLedgerCardData(string glcode, int repyear, int startmonth, int endmonth)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(glcode))
                    return BadRequest("glcode is required.");

                if (repyear < 2000 || repyear > 2100)
                    return BadRequest("Invalid repyear. Must be between 2000 and 2100.");

                if (startmonth < 1 || startmonth > 12)
                    return BadRequest("startmonth must be between 1 and 12.");

                if (endmonth < 1 || endmonth > 12)
                    return BadRequest("endmonth must be between 1 and 12.");

                if (startmonth > endmonth)
                    return BadRequest("startmonth cannot be greater than endmonth.");

                // Get data from repository
                var data = _repository.GetLedgerCardData(glcode.Trim(), repyear, startmonth, endmonth);

                // Calculate totals
                decimal totalDebit = 0;
                decimal totalCredit = 0;
                foreach (var item in data)
                {
                    totalDebit += item.DrAmt ?? 0;
                    totalCredit += item.CrAmt ?? 0;
                }

                // Return response with summary
                return Ok(new
                {
                    success = true,
                    message = data.Count > 0 ? "Data retrieved successfully" : "No records found for the given criteria",
                    data = data,
                    summary = new
                    {
                        glCode = glcode.Trim(),
                        accountName = data.Count > 0 ? data[0].AcName : null,
                        costCenter = data.Count > 0 ? data[0].CctName : null,
                        reportYear = repyear,
                        period = $"{startmonth:D2} to {endmonth:D2}",
                        periodDisplay = $"{GetMonthName(startmonth)} to {GetMonthName(endmonth)} {repyear}",
                        openingBalance = data.Count > 0 ? data[0].OpeningBalance : null,
                        closingBalance = data.Count > 0 ? data[0].ClosingBalance : null,
                        totalRecords = data.Count,
                        totalDebit = totalDebit,
                        totalCredit = totalCredit,
                        netMovement = totalDebit - totalCredit
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LedgerCardController.GetReport: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Error fetching ledger card data: {ex.Message}", ex));
            }
        }

        private string GetMonthName(int month)
        {
            return new DateTime(2000, month, 1).ToString("MMMM");
        }
    }
}