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
    [RoutePrefix("api/joballocatedestimatesdetails")]
    public class JobAllocatedEstimatesDetailsController : ApiController
    {
        private readonly JobAllocatedEstimatesDetailsDAL _dal = new JobAllocatedEstimatesDetailsDAL();

        // QUERY: /api/joballocatedestimatesdetails/report?costCtr=511.20&matCode=
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string costCtr, [FromUri] string matCode = "")
        {
            return ExecuteQuery(costCtr, matCode);
        }

        private IHttpActionResult ExecuteQuery(string costCtr, string matCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                var data = _dal.GetJobAllocatedEstimatesDetails(costCtr.Trim(), (matCode ?? string.Empty).Trim());
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    costCtr = costCtr.Trim(),
                    matCode = (matCode ?? string.Empty).Trim(),
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