using System;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provincecashbookinquiry")]
    public class ProvinceCashBookInquiryController : ApiController
    {
        private readonly ProvinceCashBookInquiryDAL _dal = new ProvinceCashBookInquiryDAL();

        // PATH: /api/provincecashbookinquiry/report/2026-01-01/100
        [HttpGet]
        [Route("report/{curDate}/{compId}")]
        public IHttpActionResult GetReport(string curDate, string compId)
        {
            return ExecuteQuery(curDate, compId);
        }

        // QUERY: /api/provincecashbookinquiry/report?curDate=2026-01-01&compId=100
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string curDate, [FromUri] string compId)
        {
            return ExecuteQuery(curDate, compId);
        }

        private IHttpActionResult ExecuteQuery(string curDate, string compId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(curDate) || !DateTime.TryParse(curDate, out DateTime curDt))
                    return BadRequest("curDate must be a valid date (e.g., 2026-01-01).");
                if (string.IsNullOrWhiteSpace(compId))
                    return BadRequest("compId is required.");

                string curDateFormatted = curDt.ToString("yyyy/MM/dd");

                var data = _dal.GetProvinceCashBookInquiry(curDateFormatted, compId.Trim());
                var totalAmount = data.Sum(x => x.NonTaxabl ?? 0m);

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    curDate = curDt.ToString("yyyy-MM-dd"),
                    endDate = curDt.AddDays(7).ToString("yyyy-MM-dd"),
                    compId = compId.Trim(),
                    totalRecords = data.Count,
                    totalAmount
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