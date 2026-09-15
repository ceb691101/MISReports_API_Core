using System;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/materialreqjobwise")]
    public class MaterialReqJobwiseController : ApiController
    {
        private readonly MaterialReqJobwiseDAL _dal = new MaterialReqJobwiseDAL();

        // PATH: /api/materialreqjobwise/report/100/PRJ-2026-01
        // Also supports: /api/materialreqjobwise/report/510.20/866/2008k
        [HttpGet]
        [Route("report/{costCtr}/{*projectNo}")]
        public IHttpActionResult GetReport(string costCtr, string projectNo)
        {
            return ExecuteQuery(costCtr, projectNo);
        }

        // QUERY: /api/materialreqjobwise/report?costCtr=100&projectNo=PRJ-2026-01
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string costCtr, [FromUri] string projectNo)
        {
            return ExecuteQuery(costCtr, projectNo);
        }

        private IHttpActionResult ExecuteQuery(string costCtr, string projectNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");
                if (string.IsNullOrWhiteSpace(projectNo))
                    return BadRequest("projectNo is required.");

                var data = _dal.GetMaterialReqJobwise(costCtr.Trim(), projectNo.Trim());
                var totalReqCost = data.Sum(x => x.ReqCost ?? 0m);
                var totalIssuedVal = data.Sum(x => x.IssuedVal ?? 0m);

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    costCtr = costCtr.Trim(),
                    projectNo = projectNo.Trim(),
                    totalRecords = data.Count,
                    totalReqCost,
                    totalIssuedVal
                };

                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records ({data.Count}). Result capped at {MAX_RECORDS}. Use a narrower search.",
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