using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/chequedetailwp")]
    public class ChequeDetailWPController : ApiController
    {
        private readonly ChequeDetailWPDAL _dal = new ChequeDetailWPDAL();

        private const string DateFormat = "yyyy-MM-dd";

        // PATH: /api/chequedetailwp/report/2022-01-01/2022-03-01/510.00
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{costCtr}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string costCtr)
        {
            return ExecuteQuery(fromDate, toDate, costCtr);
        }

        // QUERY: /api/chequedetailwp/report?fromDate=2022-01-01&toDate=2022-03-01&costCtr=510.00
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
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                if (!DateTime.TryParseExact(fromDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFromDate))
                    return BadRequest($"fromDate must be a valid date in {DateFormat} format (e.g., 2022-01-01).");

                if (!DateTime.TryParseExact(toDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedToDate))
                    return BadRequest($"toDate must be a valid date in {DateFormat} format (e.g., 2022-03-01).");

                if (parsedFromDate > parsedToDate)
                    return BadRequest("fromDate cannot be later than toDate.");

                // SQL expects TO_DATE(:param,'yyyy/mm/dd'), so convert here.
                string oracleFromDate = parsedFromDate.ToString("yyyy/MM/dd");
                string oracleToDate = parsedToDate.ToString("yyyy/MM/dd");

                var data = _dal.GetChequeDetailWP(oracleFromDate, oracleToDate, costCtr.Trim());
                var totalDrAmt = data.Sum(x => x.DrAmt ?? 0m);
                var totalChqAmt = data.Sum(x => x.ChqAmt ?? 0m);

                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    fromDate = parsedFromDate.ToString(DateFormat),
                    toDate = parsedToDate.ToString(DateFormat),
                    costCtr = costCtr.Trim(),
                    totalRecords = data.Count,
                    totalDrAmt,
                    totalChqAmt
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