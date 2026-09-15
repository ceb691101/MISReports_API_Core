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
    [RoutePrefix("api/ccwiseissue")]
    public class CCWiseIssueController : ApiController
    {
        private readonly CCWiseIssueDAL _dal = new CCWiseIssueDAL();
        // PATH: /api/ccwiseissue/report/2022/01/510.11
        [HttpGet]
        [Route("report/{repYear:regex(^\\d{{4}}$)}/{repMonth:regex(^\\d{{1,2}}$)}/{costCtr}")]
        public IHttpActionResult GetReport(string repYear, string repMonth, string costCtr)
        {
            return ExecuteQuery(repYear, repMonth, costCtr);
        }
        // QUERY: /api/ccwiseissue/report?repYear=2022&repMonth=1&costCtr=510.11
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string repYear = null, [FromUri] string repMonth = null, [FromUri] string costCtr = null)
        {
            return ExecuteQuery(repYear, repMonth, costCtr);
        }
        private IHttpActionResult ExecuteQuery(string repYear, string repMonth, string costCtr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repYear) || repYear.Trim().Length != 4 || !int.TryParse(repYear, out int y))
                    return BadRequest("repYear must be a 4-digit year (e.g., 2022).");
                if (string.IsNullOrWhiteSpace(repMonth) || !int.TryParse(repMonth, out int m) || m < 1 || m > 12)
                    return BadRequest("repMonth must be a valid month number (1-12).");
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");
                // yr_ind / mth_ind are numeric columns (compared unquoted in the SQL),
                // so no zero-padding here — unlike CashSheet's TO_CHAR(...,'MM') comparison.
                var data = _dal.GetCCWiseIssue(y, m, costCtr.Trim());
                var totalAmt = data.Sum(x => x.Total ?? 0m);
                var summary = new
                {
                    repYear = y,
                    repMonth = m,
                    costCtr = costCtr.Trim(),
                    totalRecords = data.Count,
                    totalAmount = totalAmt
                };
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