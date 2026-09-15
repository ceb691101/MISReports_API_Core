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
    [RoutePrefix("api/phvnonmovingbos")]
    public class PHVNonMovingBOSController : ApiController
    {
        private readonly PHVNonMovingBOSDAL _dal = new PHVNonMovingBOSDAL();

        // PATH: /api/phvnonmovingbos/report/2025/WH_CEPP
        [HttpGet]
        [Route("report/{repYear:regex(^\\d{{4}}$)}/{whCode}")]
        public IHttpActionResult GetReport(string repYear, string whCode)
        {
            return ExecuteQuery(repYear, whCode);
        }

        // QUERY: /api/phvnonmovingbos/report?repYear=2025&whCode=WH_CEPP
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string repYear, [FromUri] string whCode)
        {
            return ExecuteQuery(repYear, whCode);
        }

        private IHttpActionResult ExecuteQuery(string repYear, string whCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repYear) || repYear.Length != 4 || !int.TryParse(repYear, out _))
                    return BadRequest("repYear must be a 4-digit year (e.g., 2025).");
                if (string.IsNullOrWhiteSpace(whCode))
                    return BadRequest("whCode is required.");

                var data = _dal.GetPHVNonMovingBOS(repYear.Trim(), whCode.Trim());
                var totalStockBook = data.Sum(x => x.StockBook ?? 0m);

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    repYear = repYear.Trim(),
                    whCode = whCode.Trim(),
                    totalRecords = data.Count,
                    totalStockBook
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