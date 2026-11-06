using MISReports_Api.Compat;
using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/issuesraisedforjobs")]
    public class IssuesRaisedForJobsController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IssuesRaisedForJobsDAL _dal = new IssuesRaisedForJobsDAL();

        private const string DateFormat = "yyyy-MM-dd";

        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{matCode?}")]
        public IActionResult GetReport(string fromDate, string toDate, string matCode = null)
        {
            return ExecuteQuery(fromDate, toDate, matCode);
        }

        [HttpGet]
        [Route("report")]
        public IActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string matCode = null)
        {
            return ExecuteQuery(fromDate, toDate, matCode);
        }

        private IActionResult ExecuteQuery(string fromDate, string toDate, string matCode)
        {
            try
            {
                if (!DateTime.TryParseExact(fromDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFromDate))
                    return BadRequest($"fromDate must be a valid date in {DateFormat} format (e.g., 2026-01-01).");

                if (!DateTime.TryParseExact(toDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedToDate))
                    return BadRequest($"toDate must be a valid date in {DateFormat} format (e.g., 2026-01-31).");

                if (parsedFromDate > parsedToDate)
                    return BadRequest("fromDate cannot be later than toDate.");

                string matCodeTrimmed = (matCode ?? "").Trim();

                string oracleFromDate = parsedFromDate.ToString("yyyy/MM/dd");
                string oracleToDate = parsedToDate.ToString("yyyy/MM/dd");

                // Get all data
                var data = _dal.GetIssuesRaisedForJobs(oracleFromDate, oracleToDate, matCodeTrimmed);

                var totalQty = data.Sum(x => x.Qty ?? 0m);
                var totalIssues = data.Sum(x => x.NoOfIssues ?? 0);

                var summary = new
                {
                    fromDate = parsedFromDate.ToString(DateFormat),
                    toDate = parsedToDate.ToString(DateFormat),
                    matCode = matCodeTrimmed,
                    totalRecords = data.Count,
                    totalQty,
                    totalIssues
                };

                return Ok(new
                {
                    success = true,
                    message = data.Any() ? $"Retrieved {data.Count} records successfully" : "No records found",
                    data = data,
                    summary = summary
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ExecuteQuery failed. fromDate={fromDate}, toDate={toDate}, matCode={matCode}", fromDate, toDate, matCode);
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}