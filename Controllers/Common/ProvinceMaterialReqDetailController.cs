using MISReports_Api.Compat;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provincematerialreqdetail")]
    public class ProvinceMaterialReqDetailController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ProvinceMaterialReqDetailDAL _dal = new ProvinceMaterialReqDetailDAL();

        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{compId}")]
        public IActionResult GetReport(string fromDate, string toDate, string compId)
        {
            return ExecuteQuery(fromDate, toDate, compId);
        }

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
                if (string.IsNullOrWhiteSpace(fromDate) || !DateTime.TryParse(fromDate, out DateTime fromDt))
                    return BadRequest("fromDate must be a valid date (e.g., 2026-01-01).");
                if (string.IsNullOrWhiteSpace(toDate) || !DateTime.TryParse(toDate, out DateTime toDt))
                    return BadRequest("toDate must be a valid date (e.g., 2026-01-31).");
                if (toDt < fromDt)
                    return BadRequest("toDate cannot be earlier than fromDate.");
                if (string.IsNullOrWhiteSpace(compId))
                    return BadRequest("compId is required.");

                string fromDateFormatted = fromDt.ToString("yyyy/MM/dd");
                string toDateFormatted = toDt.ToString("yyyy/MM/dd");
                string normalizedCompId = compId.Trim();

                var data = _dal.GetProvinceMaterialReqDetail(fromDateFormatted, toDateFormatted, normalizedCompId);
                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy-MM-dd"),
                    toDate = toDt.ToString("yyyy-MM-dd"),
                    compId = normalizedCompId,
                    totalRecords = data.Count
                };

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
