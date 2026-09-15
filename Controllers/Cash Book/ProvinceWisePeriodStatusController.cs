// Controllers/ProvinceWisePeriodStatusController.cs
using System;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provincewiseperiodstatus")]
    public class ProvinceWisePeriodStatusController : ApiController
    {
        private readonly ProvinceWisePeriodStatusDAL _dal = new ProvinceWisePeriodStatusDAL();

        // PATH: /api/provincewiseperiodstatus/report/2026/7/CC
        [HttpGet]
        [Route("report/{repYear:int}/{repMonth:int}/{compId}")]
        public IHttpActionResult GetReport(int repYear, int repMonth, string compId)
        {
            return ExecuteQuery(repYear, repMonth, compId);
        }

        // QUERY: /api/provincewiseperiodstatus/report?repYear=2026&repMonth=7&compId=CC
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] int repYear, [FromUri] int repMonth, [FromUri] string compId)
        {
            return ExecuteQuery(repYear, repMonth, compId);
        }

        private IHttpActionResult ExecuteQuery(int repYear, int repMonth, string compId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(compId))
                    return BadRequest("compId is required.");

                if (repYear < 1900 || repYear > 2100)
                    return BadRequest("repYear must be a valid 4-digit year.");

                if (repMonth < 1 || repMonth > 12)
                    return BadRequest("repMonth must be between 1 and 12.");

                var data = _dal.GetProvinceWisePeriodStatus(repYear, repMonth, compId.Trim());

                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    repYear,
                    repMonth,
                    compId = compId.Trim(),
                    compName = data.FirstOrDefault()?.CompNm,
                    totalRecords = data.Count
                };

                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records ({data.Count}). Result capped at {MAX_RECORDS}. Use a more specific filter.",
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