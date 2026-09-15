using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/issuereceiptsummary")]
    public class IssueReceiptSummaryController : ApiController
    {
        private readonly IssueReceiptSummaryDAL _dal = new IssueReceiptSummaryDAL();

        private const string DateFormat = "yyyy-MM-dd"; // e.g. 2026-07-01 (dashes so it's URL/route safe)

        // PATH: /api/issuereceiptsummary/report/2026-07-01/2026-08-01/510.00
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{costCtr}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string costCtr)
        {
            return ExecuteQuery(fromDate, toDate, costCtr);
        }

        // QUERY: /api/issuereceiptsummary/report?fromDate=2026-07-01&toDate=2026-08-01&costCtr=510.00
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string costCtr)
        {
            return ExecuteQuery(fromDate, toDate, costCtr);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string costCtr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                if (!DateTime.TryParseExact(fromDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFromDate))
                    return BadRequest($"fromDate must be a valid date in {DateFormat} format (e.g., 2026-07-01).");

                if (!DateTime.TryParseExact(toDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedToDate))
                    return BadRequest($"toDate must be a valid date in {DateFormat} format (e.g., 2026-08-01).");

                if (parsedFromDate > parsedToDate)
                    return BadRequest("fromDate cannot be later than toDate.");

                // SQL expects TO_DATE(:param,'yyyy/mm/dd'), so convert here.
                string oracleFromDate = parsedFromDate.ToString("yyyy/MM/dd");
                string oracleToDate = parsedToDate.ToString("yyyy/MM/dd");

                var data = _dal.GetIssueReceiptSummary(oracleFromDate, oracleToDate, costCtr.Trim());
                var totalTrxVal = data.Sum(x => x.TrxVal ?? 0m);

                const int MAX_RECORDS = 5000;

                // Per-category subtotals, so the frontend doesn't have to recompute grouping logic.
                var categoryTotals = data
                    .GroupBy(x => x.Category)
                    .Select(g => new
                    {
                        category = g.Key,
                        recordCount = g.Count(),
                        total = g.Sum(x => x.TrxVal ?? 0m)
                    })
                    .ToList();

                var summary = new
                {
                    fromDate = parsedFromDate.ToString(DateFormat),
                    toDate = parsedToDate.ToString(DateFormat),
                    costCtr = costCtr.Trim(),
                    totalRecords = data.Count,
                    totalTrxVal,
                    categoryTotals
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