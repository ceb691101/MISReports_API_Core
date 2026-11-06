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
    [RoutePrefix("api/constructionall")]
    public class ConstructionAllController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ConstructionAllDAL _dal = new ConstructionAllDAL();

        // PATH: /api/constructionall/report/430.20
        [HttpGet]
        [Route("report/{costCtr}")]
        public IActionResult GetReport(string costCtr)
        {
            return ExecuteQuery(costCtr);
        }

        // QUERY: /api/constructionall/report?costCtr=430.20
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

                var data = _dal.GetConstructionAll(costCtr.Trim());
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    costCtr = costCtr.Trim(),
                    totalRecords = data.Count,
                    totalStdCost = data.Sum(x => x.StdCost ?? 0m)
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
                logger.Error(ex, "ExecuteQuery failed. costCtr={costCtr}", costCtr);
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}