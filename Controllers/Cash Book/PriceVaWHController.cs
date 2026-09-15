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
    [RoutePrefix("api/pricevawh")]
    public class PriceVaWHController : ApiController
    {
        private readonly PriceVaWHDAL _dal = new PriceVaWHDAL();

        // PATH: /api/pricevawh/report/2026/02/510.11/WRH-A
        [HttpGet]
        [Route("report/{repYear:regex(^\\d{{4}}$)}/{repMonth:regex(^\\d{{1,2}}$)}/{costCtr}/{whCode}")]
        public IHttpActionResult GetReport(string repYear, string repMonth, string costCtr, string whCode)
        {
            return ExecuteQuery(repYear, repMonth, costCtr, whCode);
        }

        // QUERY: /api/pricevawh/report?repYear=2026&repMonth=02&costCtr=510.11&whCode=WRH-A
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string repYear, [FromUri] string repMonth, [FromUri] string costCtr, [FromUri] string whCode)
        {
            return ExecuteQuery(repYear, repMonth, costCtr, whCode);
        }

        private IHttpActionResult ExecuteQuery(string repYear, string repMonth, string costCtr, string whCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repYear) || repYear.Length != 4 || !int.TryParse(repYear, out _))
                    return BadRequest("repYear must be a 4-digit year (e.g., 2022).");
                if (string.IsNullOrWhiteSpace(repMonth) || !int.TryParse(repMonth, out int m) || m < 1 || m > 12)
                    return BadRequest("repMonth must be a valid month number (1-12).");
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");
                if (string.IsNullOrWhiteSpace(whCode))
                    return BadRequest("whCode is required.");

                string repMonthValue = m.ToString("00");

                var data = _dal.GetPriceVaWH(repYear.Trim(), repMonthValue, costCtr.Trim(), whCode.Trim());
                var totalNetChange = data.Sum(x => x.NetChange ?? 0m);
                var totalVar = data.Sum(x => x.Var ?? 0m);

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    repYear = repYear.Trim(),
                    repMonth = repMonthValue,
                    costCtr = costCtr.Trim(),
                    whCode = whCode.Trim(),
                    totalRecords = data.Count,
                    totalNetChange,
                    totalVariance = totalVar
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
