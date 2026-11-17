using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/areat1t2t3")]
    public class AreaT1T2T3Controller : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AreaT1T2T3DAL _dal = new AreaT1T2T3DAL();

        private static readonly string[] DateFormats = { "yyyy/MM/dd", "yyyy-MM-dd" };

        // QUERY: /api/areat1t2t3/report?fromDate=2026/01/01&toDate=2026/01/31&compId=GALLE
        [HttpGet]
        [Route("report")]
        public IActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string compId)
        {
            return ExecuteQuery(fromDate, toDate, compId);
        }

        private IActionResult ExecuteQuery(string fromDate, string toDate, string compId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromDate) ||
                    !DateTime.TryParseExact(fromDate.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt))
                    return BadRequest("fromDate must be a valid date in yyyy/MM/dd format.");

                if (string.IsNullOrWhiteSpace(toDate) ||
                    !DateTime.TryParseExact(toDate.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
                    return BadRequest("toDate must be a valid date in yyyy/MM/dd format.");

                if (toDt.Date < fromDt.Date)
                    return BadRequest("toDate cannot be earlier than fromDate.");

                if (string.IsNullOrWhiteSpace(compId))
                    return BadRequest("compId is required.");

                var data = _dal.GetAreaT1T2T3(fromDt, toDt, compId.Trim());
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy/MM/dd"),
                    toDate = toDt.ToString("yyyy/MM/dd"),
                    compId = compId.Trim(),
                    totalRecords = data.Count,
                    averageT1 = data.Any(x => x.T1.HasValue) ? data.Where(x => x.T1.HasValue).Average(x => x.T1.Value) : (decimal?)null,
                    averageT2Ln = data.Any(x => x.T2Ln.HasValue) ? data.Where(x => x.T2Ln.HasValue).Average(x => x.T2Ln.Value) : (decimal?)null,
                    averageT2Smc = data.Any(x => x.T2Smc.HasValue) ? data.Where(x => x.T2Smc.HasValue).Average(x => x.T2Smc.Value) : (decimal?)null,
                    averageT3 = data.Any(x => x.T3.HasValue) ? data.Where(x => x.T3.HasValue).Average(x => x.T3.Value) : (decimal?)null
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
                logger.Error(ex, "ExecuteQuery failed. fromDate={fromDate}, toDate={toDate}, compId={compId}", fromDate, toDate, compId);
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}