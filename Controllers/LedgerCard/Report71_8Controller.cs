using MISReports_Api.DAL;
using System;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api")]
    public class Report71_8Controller : ApiController
    {
        private readonly Report71_8Repository _repository = new Report71_8Repository();

        [HttpGet]
        [Route("ledgercard/report-71-8")]
        [Route("ledgercard/report718")]
        [Route("report718")]
        public IHttpActionResult GetReport(
            [FromUri] string compId = null,
            [FromUri] int repyear = 0,
            [FromUri] int repmonth = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(compId))
                {
                    return BadRequest("Parameter 'compId' (Division/Region/Company ID) is required.");
                }

                if (repyear < 2000 || repyear > 2100)
                {
                    return BadRequest("Parameter 'repyear' must be between 2000 and 2100.");
                }

                if (repmonth < 1 || repmonth > 12)
                {
                    return BadRequest("Parameter 'repmonth' must be between 1 and 12.");
                }

                var data = _repository.GetReport71_8Data(compId.Trim(), repyear, repmonth);

                decimal totalDebit = data.Sum(x => x.DrAmt ?? 0m);
                decimal totalCredit = data.Sum(x => x.CrAmt ?? 0m);
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
                        compId = compId.Trim(),
                        cctName = first?.CctName ?? string.Empty,
                        repYear = repyear,
                        repMonth = repmonth,
                        periodDisplay = $"{GetMonthName(repmonth)} / {repyear}",
                        totalRecords = data.Count,
                        totalDebit = totalDebit,
                        totalCredit = totalCredit,
                        netMovement = totalDebit - totalCredit
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Report71_8Controller: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Error fetching 71/8 Report data: {ex.Message}", ex));
            }
        }

        private string GetMonthName(int month)
        {
            try
            {
                return new DateTime(2000, month, 1).ToString("MMMM");
            }
            catch
            {
                return month.ToString();
            }
        }
    }
}
