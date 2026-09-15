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
    [RoutePrefix("api/regionperiodstatus")]
    public class RegionPeriodStatusController : ApiController
    {
        private readonly RegionPeriodStatusDAL _dal = new RegionPeriodStatusDAL();
        // PATH: /api/regionperiodstatus/report/2026/07/DISCO1
        [HttpGet]
        [Route("report/{repYear:regex(^\\d{{4}}$)}/{repMonth:regex(^\\d{{1,2}}$)}/{region}")]
        public IHttpActionResult GetReport(string repYear, string repMonth, string region)
        {
            return ExecuteQuery(repYear, repMonth, region);
        }
        // QUERY: /api/regionperiodstatus/report?repYear=2026&repMonth=07&region=DISCO1
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string repYear, [FromUri] string repMonth, [FromUri] string region)
        {
            return ExecuteQuery(repYear, repMonth, region);
        }
        private IHttpActionResult ExecuteQuery(string repYear, string repMonth, string region)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repYear) || repYear.Length != 4 || !int.TryParse(repYear, out _))
                    return BadRequest("repYear must be a 4-digit year (e.g., 2026).");
                if (string.IsNullOrWhiteSpace(repMonth) || !int.TryParse(repMonth, out int m) || m < 1 || m > 13)
                    return BadRequest("repMonth must be a valid period number (1-13).");
                if (string.IsNullOrWhiteSpace(region))
                    return BadRequest("region is required.");
                
                string repMonthTrim = m.ToString();
                var data = _dal.GetRegionPeriodStatus(repYear.Trim(), repMonthTrim, region.Trim());
                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    repYear = repYear.Trim(),
                    repMonth = repMonthTrim,
                    region = region.Trim(),
                    regionName = data.FirstOrDefault()?.CompNm,
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