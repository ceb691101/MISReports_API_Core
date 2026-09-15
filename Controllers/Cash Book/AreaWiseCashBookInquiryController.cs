using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/areawisecashbookinquiry")]
    public class AreaWiseCashBookInquiryController : ApiController
    {
        private readonly AreaWiseCashBookInquiryDAL _dal = new AreaWiseCashBookInquiryDAL();

        private const string DateFormat = "yyyy-MM-dd"; // e.g. 2026-01-08 (dashes so it's URL/route safe)

        // PATH: /api/areawisecashbookinquiry/report/2026-01-08/2026-08-09/100
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{area}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string area)
        {
            return ExecuteQuery(fromDate, toDate, area);
        }

        // QUERY: /api/areawisecashbookinquiry/report?fromDate=2026-01-08&toDate=2026-08-09&area=100
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string area)
        {
            return ExecuteQuery(fromDate, toDate, area);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string area)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(area))
                    return BadRequest("area is required.");

                if (!DateTime.TryParseExact(fromDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFromDate))
                    return BadRequest($"fromDate must be a valid date in {DateFormat} format (e.g., 2026-01-08).");

                if (!DateTime.TryParseExact(toDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedToDate))
                    return BadRequest($"toDate must be a valid date in {DateFormat} format (e.g., 2026-08-09).");

                if (parsedFromDate > parsedToDate)
                    return BadRequest("fromDate cannot be later than toDate.");

                // SQL expects TO_DATE(:param,'yyyy/mm/dd'), so convert here.
                string oracleFromDate = parsedFromDate.ToString("yyyy/MM/dd");
                string oracleToDate = parsedToDate.ToString("yyyy/MM/dd");

                var data = _dal.GetAreaWiseCashBookInquiry(oracleFromDate, oracleToDate, area.Trim());

                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    fromDate = parsedFromDate.ToString(DateFormat),
                    toDate = parsedToDate.ToString(DateFormat),
                    area = area.Trim(),
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