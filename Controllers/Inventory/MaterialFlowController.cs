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
    [RoutePrefix("api/materialflow")]
    public class MaterialFlowController : ApiController
    {
        private readonly MaterialFlowDAL _dal = new MaterialFlowDAL();

        // PATH: /api/materialflow/report/2026-05-09/2026-07-09/510.00/EBG120007G/A/WH_CEPP
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{costCtr}/{matCode}/{grCode}/{whCode}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string costCtr, string matCode, string grCode, string whCode)
        {
            return ExecuteQuery(fromDate, toDate, costCtr, matCode, grCode, whCode);
        }

        // QUERY: /api/materialflow/report?fromDate=2026-05-09&toDate=2026-07-09&costCtr=510.00&matCode=EBG120007G&grCode=A&whCode=WH_CEPP
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery(
            [FromUri] string fromDate, [FromUri] string toDate, [FromUri] string costCtr,
            [FromUri] string matCode, [FromUri] string grCode, [FromUri] string whCode)
        {
            return ExecuteQuery(fromDate, toDate, costCtr, matCode, grCode, whCode);
        }

        // GET: /api/materialflow/gradecodes
        [HttpGet]
        [Route("gradecodes")]
        public IHttpActionResult GetGradeCodes()
        {
            try
            {
                var codes = _dal.GetDistinctGradeCodes();
                return Ok(new { success = true, data = codes });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string costCtr, string matCode, string grCode, string whCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromDate) || !DateTime.TryParse(fromDate, out DateTime fromDt))
                    return BadRequest("fromDate must be a valid date (e.g., 2026-05-09).");
                if (string.IsNullOrWhiteSpace(toDate) || !DateTime.TryParse(toDate, out DateTime toDt))
                    return BadRequest("toDate must be a valid date (e.g., 2026-07-09).");
                if (toDt < fromDt)
                    return BadRequest("toDate cannot be earlier than fromDate.");
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");
                if (string.IsNullOrWhiteSpace(matCode))
                    return BadRequest("matCode is required.");
                if (string.IsNullOrWhiteSpace(grCode))
                    return BadRequest("grCode is required.");
                if (string.IsNullOrWhiteSpace(whCode))
                    return BadRequest("whCode is required.");

                // SQL uses TO_DATE(:param,'yyyy/mm/dd'), so format to match the mask.
                string fromDateFormatted = fromDt.ToString("yyyy/MM/dd");
                string toDateFormatted = toDt.ToString("yyyy/MM/dd");

                var data = _dal.GetMaterialFlow(
                    fromDateFormatted, toDateFormatted, costCtr.Trim(),
                    matCode.Trim(), grCode.Trim(), whCode.Trim());

                // QtyOnHandP / QIn / QOut / CIn / COut are scalar values (same on every
                // row) computed by the SQL's correlated subqueries, so take them from the
                // first row if present.
                decimal qtyOnHand = data.FirstOrDefault()?.QtyOnHandP ?? 0m;
                decimal qIn = data.FirstOrDefault()?.QIn ?? 0m;
                decimal qOut = data.FirstOrDefault()?.QOut ?? 0m;
                decimal cIn = data.FirstOrDefault()?.CIn ?? 0m;
                decimal cOut = data.FirstOrDefault()?.COut ?? 0m;

                decimal openingQty = qtyOnHand + qIn + qOut;
                decimal closingQty = qtyOnHand + cIn + cOut;

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy-MM-dd"),
                    toDate = toDt.ToString("yyyy-MM-dd"),
                    costCtr = costCtr.Trim(),
                    matCode = matCode.Trim(),
                    grCode = grCode.Trim(),
                    whCode = whCode.Trim(),
                    totalRecords = data.Count,
                    qtyOnHand,
                    openingQty,
                    closingQty
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