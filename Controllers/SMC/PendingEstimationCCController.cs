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
    [RoutePrefix("api/pendingestimationcc")]
    public class PendingEstimationCCController : ApiController
    {
        private readonly PendingEstimationCCDAL _dal = new PendingEstimationCCDAL();

        private static readonly string[] DateFormats = { "yyyy/MM/dd", "yyyy-MM-dd" };

        // QUERY: /api/pendingestimationcc/report?fromDate=2026/01/01&toDate=2026/01/31&costCtr=511.20&jobType=CR
        // jobType is optional
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery(
            [FromUri] string fromDate,
            [FromUri] string toDate,
            [FromUri] string costCtr,
            [FromUri] string jobType = null)
        {
            return ExecuteQuery(fromDate, toDate, costCtr, jobType);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string costCtr, string jobType)
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

                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                string jobTypeTrimmed = string.IsNullOrWhiteSpace(jobType) ? null : jobType.Trim();

                var data = _dal.GetPendingEstimationCC(fromDt, toDt, costCtr.Trim(), jobTypeTrimmed);
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy/MM/dd"),
                    toDate = toDt.ToString("yyyy/MM/dd"),
                    costCtr = costCtr.Trim(),
                    jobType = jobTypeTrimmed ?? "ALL",
                    totalRecords = data.Count
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