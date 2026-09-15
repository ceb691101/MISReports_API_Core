using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/cashsheet")]
    public class CashSheetController : ApiController
    {
        private readonly CashSheetDAL _dal = new CashSheetDAL();

        // PATH: /api/cashsheet/report/year/month/costctr
        [HttpGet]
        [Route("report/{repYear:regex(^\\d{{4}}$)}/{repMonth:regex(^\\d{{1,2}}$)}/{costCtr}")]
        public IHttpActionResult GetReport(string repYear, string repMonth, string costCtr)
        {
            return ExecuteQuery(repYear, repMonth, costCtr);
        }

        // QUERY: /api/cashsheet/report?repYear=2022&repMonth=5&costCtr=510.00
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string repYear, [FromUri] string repMonth, [FromUri] string costCtr)
        {
            return ExecuteQuery(repYear, repMonth, costCtr);
        }

        private IHttpActionResult ExecuteQuery(string repYear, string repMonth, string costCtr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repYear) || repYear.Length != 4 || !int.TryParse(repYear, out _))
                    return BadRequest("repYear must be a 4-digit year (e.g., 2022).");

                if (string.IsNullOrWhiteSpace(repMonth) || !int.TryParse(repMonth, out int m) || m < 1 || m > 12)
                    return BadRequest("repMonth must be a valid month number (1-12).");

                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                // SQL compares TO_CHAR(chq_dt,'MM') which is always 2 digits, so pad here.
                string repMonthPadded = m.ToString("00");

                var data = _dal.GetCashSheet(repYear.Trim(), repMonthPadded, costCtr.Trim());
                var totalAmt = data.Sum(x => x.ChqAmt ?? 0m);
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    repYear = repYear.Trim(),
                    repMonth = repMonthPadded,
                    costCtr = costCtr.Trim(),
                    totalRecords = data.Count,
                    totalAmount = totalAmt
                };

                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records ({data.Count}). Result capped at {MAX_RECORDS}. Use narrower filters.",
                        data = new object[0],
                        summary
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = data.Any() ? "Data retrieved successfully" : "No records found",
                    data,
                    summary
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}