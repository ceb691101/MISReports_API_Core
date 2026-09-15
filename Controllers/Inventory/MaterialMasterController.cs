using System;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/materialmaster")]
    public class MaterialMasterController : ApiController
    {
        private readonly MaterialMasterDAL _dal = new MaterialMasterDAL();

        // PATH:
        // /api/materialmaster/report/RM-1001
        // /api/materialmaster/report/RM-1001?status=2
        [HttpGet]
        [Route("report/{matCode}")]
        public IHttpActionResult GetReport(string matCode, [FromUri] int? status = null)
        {
            return ExecuteQuery(matCode, status);
        }

        // QUERY:
        // /api/materialmaster/report
        // /api/materialmaster/report?matCode=RM-1001
        // /api/materialmaster/report?status=2
        // /api/materialmaster/report?matCode=RM-1001&status=2
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string matCode = null, [FromUri] int? status = null)
        {
            return ExecuteQuery(matCode, status);
        }

        private IHttpActionResult ExecuteQuery(string matCode, int? status)
        {
            try
            {
                string matCodeTrimmed = string.IsNullOrWhiteSpace(matCode)
                    ? null
                    : matCode.Trim();

                // Allow only 2 or 3
                if (status.HasValue && status != 2 && status != 3)
                {
                    return BadRequest("Status must be 2 (Active) or 3 (Inactive).");
                }

                var data = _dal.GetMaterialMaster(matCodeTrimmed, status);

                var summary = new
                {
                    matCode = matCodeTrimmed ?? "(all)",
                    status = status.HasValue
                        ? (status == 2 ? "Active" : "Inactive")
                        : "Both",
                    totalRecords = data.Count
                };

                return Ok(new
                {
                    success = true,
                    message = data.Any()
                        ? "Data retrieved successfully"
                        : "No records found",
                    data,
                    summary
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");

                return InternalServerError(
                    new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}