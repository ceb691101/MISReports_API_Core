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
    [RoutePrefix("api/constructioncompleted")]
    public class ConstructionCompletedController : ApiController
    {
        private readonly ConstructionCompletedDAL _dal = new ConstructionCompletedDAL();

        // PATH: /api/constructioncompleted/report/ALL/COLOMBO/510.00
        [HttpGet]
        [Route("report/{fundId}/{district}/{costCtr}")]
        public IHttpActionResult GetReport(string fundId, string district, string costCtr)
        {
            return ExecuteQuery(costCtr, fundId, district, string.Empty);
        }

        // QUERY: /api/constructioncompleted/report?fundId=ALL&district=COLOMBO&costCtr=510.00&csc=
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string fundId, [FromUri] string district, [FromUri] string costCtr, [FromUri] string csc = "")
        {
            return ExecuteQuery(costCtr, fundId, district, csc);
        }

        private IHttpActionResult ExecuteQuery(string costCtr, string fundId, string district, string csc)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                // "ALL" (or blank) means match every fund / district, since the SQL uses
                // TRIM(...) LIKE '%' || :param || '%', and an empty param matches everything.
                string fundIdParam = NormalizeAllFilter(fundId);
                string districtParam = NormalizeAllFilter(district);
                string cscParam = string.IsNullOrWhiteSpace(csc) ? string.Empty : csc.Trim();

                var data = _dal.GetConstructionCompleted(costCtr.Trim(), fundIdParam, districtParam, cscParam);
                var inProgressCount = _dal.GetInProgressCount(costCtr.Trim(), fundIdParam);

                var totalStdCost = data.Sum(x => x.StdCost ?? 0m);
                var totalWp = data.Sum(x => x.Wp ?? 0m);

                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    costCtr = costCtr.Trim(),
                    fundId = string.IsNullOrEmpty(fundIdParam) ? "ALL" : fundIdParam,
                    district = string.IsNullOrEmpty(districtParam) ? "ALL" : districtParam,
                    totalRecords = data.Count,
                    totalStdCost,
                    totalWp,
                    completedJobs = data.Count,
                    inProgressJobs = inProgressCount
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

        private static string NormalizeAllFilter(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("ALL", StringComparison.OrdinalIgnoreCase))
                return string.Empty;
            return value.Trim();
        }
    }
}