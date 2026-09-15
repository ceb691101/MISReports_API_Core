using System;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/transactionspercostcenter")]
    public class TransactionsPerCostCenterController : ApiController
    {
        private readonly TransactionsPerCostCenterDAL _dal = new TransactionsPerCostCenterDAL();

        // PATH: /api/transactionspercostcenter/report/2026-01-01/2026-01-31/100
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{costCtr}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string costCtr)
        {
            return ExecuteQuery(fromDate, toDate, costCtr);
        }

        // QUERY: /api/transactionspercostcenter/report?fromDate=2026-01-01&toDate=2026-01-31&costCtr=100
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string costCtr)
        {
            return ExecuteQuery(fromDate, toDate, costCtr);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string costCtr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromDate) || !DateTime.TryParse(fromDate, out DateTime fromDt))
                    return BadRequest("fromDate must be a valid date (e.g., 2026-01-01).");
                if (string.IsNullOrWhiteSpace(toDate) || !DateTime.TryParse(toDate, out DateTime toDt))
                    return BadRequest("toDate must be a valid date (e.g., 2026-01-31).");
                if (toDt < fromDt)
                    return BadRequest("toDate cannot be earlier than fromDate.");
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                string fromDateFormatted = fromDt.ToString("yyyy/MM/dd");
                string toDateFormatted = toDt.ToString("yyyy/MM/dd");

                var data = _dal.GetTransactionsPerCostCenter(fromDateFormatted, toDateFormatted, costCtr.Trim());
                var totalCount = data.Sum(x => x.Count);

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy-MM-dd"),
                    toDate = toDt.ToString("yyyy-MM-dd"),
                    costCtr = costCtr.Trim(),
                    totalRecords = data.Count,
                    totalCount
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