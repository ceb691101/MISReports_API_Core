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
    [RoutePrefix("api/chequedetailsexp")]
    public class ChequeDetailsExpController : ApiController
    {
        private readonly ChequeDetailsExpDAL _dal = new ChequeDetailsExpDAL();

        private const string DATE_FORMAT = "yyyy/MM/dd";

        // QUERY: /api/chequedetailsexp/report?costCtr=A3900&acctCode=510.00&fromDate=2026/01/01&toDate=2026/04/30
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string costCtr, [FromUri] string acctCode, [FromUri] string fromDate, [FromUri] string toDate)
        {
            return ExecuteQuery(costCtr, acctCode, fromDate, toDate);
        }

        // PATH: /api/chequedetailsexp/report/costctr/acctcode/fromdate/todate
        [HttpGet]
        [Route("report/{costCtr}/{acctCode}/{fromDate}/{toDate}")]
        public IHttpActionResult GetReport(string costCtr, string acctCode, string fromDate, string toDate)
        {
            return ExecuteQuery(costCtr, acctCode, fromDate, toDate);
        }

        private IHttpActionResult ExecuteQuery(string costCtr, string acctCode, string fromDate, string toDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                if (string.IsNullOrWhiteSpace(acctCode))
                    return BadRequest("acctCode is required.");

                if (!DateTime.TryParseExact(fromDate, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt))
                    return BadRequest("fromDate must be in yyyy/mm/dd format.");

                if (!DateTime.TryParseExact(toDate, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
                    return BadRequest("toDate must be in yyyy/mm/dd format.");

                if (fromDt > toDt)
                    return BadRequest("fromDate must not be later than toDate.");

                var data = _dal.GetChequeDetailsExp(costCtr.Trim(), acctCode.Trim(), fromDt.ToString(DATE_FORMAT), toDt.ToString(DATE_FORMAT));
                var totalDrAmt = data.Sum(x => x.DrAmt ?? 0m);
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    costCtr = costCtr.Trim(),
                    acctCode = acctCode.Trim(),
                    fromDate = fromDt.ToString(DATE_FORMAT),
                    toDate = toDt.ToString(DATE_FORMAT),
                    totalRecords = data.Count,
                    totalDrAmt
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