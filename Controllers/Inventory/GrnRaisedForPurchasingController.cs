// Controllers/GrnRaisedForPurchasingController.cs
using System;
using System.Globalization;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/grnraisedforpurchasing")]
    public class GrnRaisedForPurchasingController : ApiController
    {
        private readonly GrnRaisedForPurchasingDAL _dal = new GrnRaisedForPurchasingDAL();

        private const string DateFormat = "yyyy-MM-dd";

        // PATH: /api/grnraisedforpurchasing/report/2026-07-01/2026-07-14/  (matCode optional/blank = all)
        // PATH: /api/grnraisedforpurchasing/report/2026-07-01/2026-07-14/AB
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{matCode?}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string matCode = null)
        {
            return ExecuteQuery(fromDate, toDate, matCode);
        }

        // QUERY: /api/grnraisedforpurchasing/report?fromDate=2026-07-01&toDate=2026-07-14&matCode=AB
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string matCode = null)
        {
            return ExecuteQuery(fromDate, toDate, matCode);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string matCode)
        {
            try
            {
                if (!DateTime.TryParseExact(fromDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFromDate))
                    return BadRequest($"fromDate must be a valid date in {DateFormat} format (e.g., 2026-07-01).");

                if (!DateTime.TryParseExact(toDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedToDate))
                    return BadRequest($"toDate must be a valid date in {DateFormat} format (e.g., 2026-07-14).");

                if (parsedFromDate > parsedToDate)
                    return BadRequest("fromDate cannot be later than toDate.");

                // Blank/omitted matCode is valid — means "all materials".
                string matCodeTrimmed = (matCode ?? "").Trim();

                string oracleFromDate = parsedFromDate.ToString("yyyy/MM/dd");
                string oracleToDate = parsedToDate.ToString("yyyy/MM/dd");

                var data = _dal.GetGrnRaisedForPurchasing(oracleFromDate, oracleToDate, matCodeTrimmed);
                var periodSummary = _dal.GetPeriodSummary(oracleFromDate, oracleToDate);

                var totalGrnValue = data.Sum(x => x.Value ?? 0m);
                var totalQty = data.Sum(x => x.Qty ?? 0m);

                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    fromDate = parsedFromDate.ToString(DateFormat),
                    toDate = parsedToDate.ToString(DateFormat),
                    matCode = matCodeTrimmed,
                    totalRecords = data.Count,
                    totalQty,
                    totalGrnValue,           // respects matCode filter
                    grnCount = periodSummary.GrnCount,       // * all materials, period-wide
                    issueCount = periodSummary.IssueCount,   // * all materials, period-wide
                    issueTotal = periodSummary.IssueTotal    // * all materials, period-wide
                };

                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records ({data.Count}). Result capped at {MAX_RECORDS}. Use a more specific material code.",
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