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
    [RoutePrefix("api/constructionall")]
    public class ConstructionAllController : ApiController
    {
        private readonly ConstructionAllDAL _dal = new ConstructionAllDAL();

        // PATH: /api/constructionall/report/430.20
        [HttpGet]
        [Route("report/{costCtr}")]
        public IHttpActionResult GetReport(string costCtr)
        {
            return ExecuteQuery(costCtr);
        }

        // QUERY: /api/constructionall/report?costCtr=430.20
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string costCtr)
        {
            return ExecuteQuery(costCtr);
        }

        private IHttpActionResult ExecuteQuery(string costCtr)
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
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}