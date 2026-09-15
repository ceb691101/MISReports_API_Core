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
    [RoutePrefix("api/materialrequisitionwithissuedetails")]
    public class MaterialRequisitionWithIssueDetailsController : ApiController
    {
        private readonly MaterialRequisitionWithIssueDetailsDAL _dal = new MaterialRequisitionWithIssueDetailsDAL();

        private const string DateFormat = "yyyy-MM-dd"; // e.g. 2026-08-01 (dashes so it's URL/route safe)

        // PATH: /api/materialrequisitionwithissuedetails/report/2026-08-01/2026-08-15/510.00/EBG12
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{costCtr}/{matCode}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string costCtr, string matCode)
        {
            return ExecuteQuery(fromDate, toDate, costCtr, matCode);
        }

        // QUERY: /api/materialrequisitionwithissuedetails/report?fromDate=2026-08-01&toDate=2026-08-15&costCtr=510.00&matCode=EBG12
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery(
            [FromUri] string fromDate,
            [FromUri] string toDate,
            [FromUri] string costCtr,
            [FromUri] string matCode = "")
        {
            return ExecuteQuery(fromDate, toDate, costCtr, matCode);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string costCtr, string matCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");

                if (!DateTime.TryParseExact(fromDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFromDate))
                    return BadRequest($"fromDate must be a valid date in {DateFormat} format (e.g., 2026-08-01).");

                if (!DateTime.TryParseExact(toDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedToDate))
                    return BadRequest($"toDate must be a valid date in {DateFormat} format (e.g., 2026-08-15).");

                if (parsedFromDate > parsedToDate)
                    return BadRequest("fromDate cannot be later than toDate.");

                // SQL expects TO_DATE(:param,'yyyy/mm/dd'), so convert here.
                string oracleFromDate = parsedFromDate.ToString("yyyy/MM/dd");
                string oracleToDate = parsedToDate.ToString("yyyy/MM/dd");

                // matCode is a prefix match (TRIM(res_cd) LIKE :matcode || '%'); blank matches all.
                string matCodeParam = string.IsNullOrWhiteSpace(matCode) || matCode.Trim().Equals("ALL", StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : matCode.Trim();

                var data = _dal.GetMaterialRequisitionWithIssueDetails(oracleFromDate, oracleToDate, costCtr.Trim(), matCodeParam);

                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    fromDate = parsedFromDate.ToString(DateFormat),
                    toDate = parsedToDate.ToString(DateFormat),
                    costCtr = costCtr.Trim(),
                    matCode = string.IsNullOrEmpty(matCodeParam) ? "ALL" : matCodeParam,
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