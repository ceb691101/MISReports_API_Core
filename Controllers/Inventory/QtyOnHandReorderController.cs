using MISReports_Api.Compat;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/qtyonhandreorder")]
    public class QtyOnHandReorderController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly QtyOnHandReorderDAL _dal = new QtyOnHandReorderDAL();

        // PATH: /api/qtyonhandreorder/report/100
        [HttpGet]
        [Route("report/{costCtr}")]
        public IActionResult GetReport(string costCtr)
        {
            return ExecuteQuery(costCtr);
        }

        // QUERY: /api/qtyonhandreorder/report?costCtr=100
        [HttpGet]
        [Route("report")]
        public IActionResult GetQuery([FromUri] string costCtr)
        {
            return ExecuteQuery(costCtr);
        }

        private IActionResult ExecuteQuery(string costCtr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                var data = _dal.GetQtyOnHandReorder(costCtr.Trim());

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    costCtr = costCtr.Trim(),
                    totalRecords = data.Count
                };

                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records ({data.Count}). Result capped at {MAX_RECORDS}.",
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
                logger.Error(ex, "ExecuteQuery failed. costCtr={costCtr}", costCtr);
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}