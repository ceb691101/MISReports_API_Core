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
    [RoutePrefix("api/bulkconnectiondetails")]
    public class BulkConnectionDetailsController : ApiController
    {
        private readonly BulkConnectionDetailsDAL _dal = new BulkConnectionDetailsDAL();

        private static readonly string[] DateFormats = { "yyyy/MM/dd", "yyyy-MM-dd" };

        // QUERY: /api/bulkconnectiondetails/report?fromDate=2026/01/01&toDate=2026/01/31
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate)
        {
            return ExecuteQuery(fromDate, toDate);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate)
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

                var data = _dal.GetBulkConnectionDetails(fromDt, toDt);
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy/MM/dd"),
                    toDate = toDt.ToString("yyyy/MM/dd"),
                    totalRecords = data.Count,
                    totalStandardCost = data.Sum(x => x.StandardCost ?? 0m),
                    totalConsumerPayable = data.Sum(x => x.ConsumerPayable ?? 0m),
                    totalWorkEstimateActualCost = data.Sum(x => x.WorkEstimateActualCost ?? 0m)
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