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
    [RoutePrefix("api/phvdamagebos")]
    public class PHVDamageBOSController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PHVDamageBOSDAL _dal = new PHVDamageBOSDAL();

        // PATH: /api/phvdamagebos/report/2025/WH_CELIFT
        [HttpGet]
        [Route("report/{repYear:regex(^\\d{{4}}$)}/{whCode}")]
        public IActionResult GetReport(string repYear, string whCode)
        {
            return ExecuteQuery(repYear, whCode);
        }

        // QUERY: /api/phvdamagebos/report?repYear=2025&whCode=WH_CELIFT
        [HttpGet]
        [Route("report")]
        public IActionResult GetQuery([FromUri] string repYear, [FromUri] string whCode)
        {
            return ExecuteQuery(repYear, whCode);
        }

        private IActionResult ExecuteQuery(string repYear, string whCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repYear) || repYear.Length != 4 || !int.TryParse(repYear, out _))
                    return BadRequest("repYear must be a 4-digit year (e.g., 2025).");
                if (string.IsNullOrWhiteSpace(whCode))
                    return BadRequest("whCode is required.");

                var data = _dal.GetPHVDamageBOS(repYear.Trim(), whCode.Trim());
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
                logger.Error(ex, "ExecuteQuery failed. repYear={repYear}, whCode={whCode}", repYear, whCode);
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}