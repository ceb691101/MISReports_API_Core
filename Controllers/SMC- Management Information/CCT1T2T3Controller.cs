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
    [RoutePrefix("api/cct1t2t3")]
    public class CCT1T2T3Controller : ApiController
    {
        private readonly CCT1T2T3DAL _dal = new CCT1T2T3DAL();

        private static readonly string[] DateFormats = { "yyyy/MM/dd", "yyyy-MM-dd" };

        // QUERY: /api/cct1t2t3/report?fromDate=2026/05/01&toDate=2026/06/01&costCtr=511.20
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

                var data = _dal.GetCCT1T2T3(fromDt, toDt, costCtr.Trim());
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy/MM/dd"),
                    toDate = toDt.ToString("yyyy/MM/dd"),
                    costCtr = costCtr.Trim(),
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
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}