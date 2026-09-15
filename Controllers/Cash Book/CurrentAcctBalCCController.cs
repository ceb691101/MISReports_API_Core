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
    [RoutePrefix("api/currentacctbalcc")]
    public class CurrentAcctBalCCController : ApiController
    {
        private readonly CurrentAcctBalCCDAL _dal = new CurrentAcctBalCCDAL();

        // PATH: /api/currentacctbalcc/report/year/month/costctr
        [HttpGet]
        [Route("report/{repYear:regex(^\\d{{4}}$)}/{repMonth:regex(^\\d{{1,2}}$)}/{costCtr}")]
        public IHttpActionResult GetReport(string repYear, string repMonth, string costCtr)
        {
            return ExecuteQuery(repYear, repMonth, costCtr);
        }

        // QUERY: /api/currentacctbalcc/report?repYear=2026&repMonth=7&costCtr=910.00
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

                string repMonthPadded = m.ToString("00");
                var data = _dal.GetCurrentAcctBalCC(repYear.Trim(), repMonthPadded, costCtr.Trim());
                var totalAmt = data.Sum(x => x.ClBal ?? 0m);
                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    repYear = repYear.Trim(),
                    repMonth = repMonthPadded,
                    costCtr = costCtr.Trim(),
                    totalRecords = data.Count,
                    totalClBal = totalAmt
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

                if (!data.Any())
                {
                    var debug = _dal.GetBoundParamDebug(repYear.Trim(), repMonthPadded, costCtr.Trim());
                    return Ok(new
                    {
                        success = true,
                        message = "No records found — BUILD-TEST-001",
                        data = new object[0],
                        summary,
                        debug
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Data retrieved successfully",
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