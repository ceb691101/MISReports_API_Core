using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ccjobsummary")]
    public class CCJobSummaryController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly CCJobSummaryDAL _dal = new CCJobSummaryDAL();

        // QUERY: /api/ccjobsummary/report?repYear=2026&costCtr=510.20&fromNo=0001&toNo=0030
        [HttpGet]
        [Route("report")]
        public IActionResult GetQuery(
            [FromUri] string repYear,
            [FromUri] string costCtr,
            [FromUri] string fromNo,
            [FromUri] string toNo)
        {
            return ExecuteQuery(repYear, costCtr, fromNo, toNo);
        }

        private IActionResult ExecuteQuery(string repYear, string costCtr, string fromNo, string toNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repYear) || repYear.Trim().Length != 4 || !int.TryParse(repYear.Trim(), out _))
                    return BadRequest("repYear must be a 4-digit year (e.g., 2026).");

                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                if (string.IsNullOrWhiteSpace(fromNo))
                    return BadRequest("fromNo is required.");

                if (string.IsNullOrWhiteSpace(toNo))
                    return BadRequest("toNo is required.");

                if (string.Compare(toNo.Trim(), fromNo.Trim(), StringComparison.Ordinal) < 0)
                    return BadRequest("toNo cannot be earlier than fromNo.");

                var data = _dal.GetCCJobSummary(repYear.Trim(), costCtr.Trim(), fromNo.Trim(), toNo.Trim());
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    repYear = repYear.Trim(),
                    costCtr = costCtr.Trim(),
                    fromNo = fromNo.Trim(),
                    toNo = toNo.Trim(),
                    totalRecords = data.Count,
                    totalTrxQty = data.Sum(x => x.TrxQty ?? 0m)
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
                logger.Error(ex, "ExecuteQuery failed. repYear={repYear}, costCtr={costCtr}, fromNo={fromNo}, toNo={toNo}", repYear, costCtr, fromNo, toNo);
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}

