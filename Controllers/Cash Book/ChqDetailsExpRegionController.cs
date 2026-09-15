using System;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/chqdetailsexpregion")]
    public class ChqDetailsExpRegionController : ApiController
    {
        private readonly ChequeDetailsExpRegionDAL _dal;

        public ChqDetailsExpRegionController()
        {
            _dal = new ChequeDetailsExpRegionDAL(GetConnectionString());
        }

        // PATH: /api/chqdetailsexpregion/report/2026-01-01/2026-01-31/DISCO1?glCode=
        // glCode is optional
        [HttpGet]
        [Route("report/{fromDate}/{toDate}/{compId}")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string compId, [FromUri] string glCode = "")
        {
            return ExecuteQuery(fromDate, toDate, compId, glCode);
        }

        // QUERY: /api/chqdetailsexpregion/report?fromDate=2026-01-01&toDate=2026-01-31&compId=DISCO1&glCode=
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string compId, [FromUri] string glCode = "")
        {
            return ExecuteQuery(fromDate, toDate, compId, glCode);
        }

        private IHttpActionResult ExecuteQuery(string fromDate, string toDate, string compId, string glCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fromDate) ||
                    !DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt))
                    return BadRequest("fromDate must be a valid date in yyyy-MM-dd format (e.g., 2026-01-01).");

                if (string.IsNullOrWhiteSpace(toDate) ||
                    !DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
                    return BadRequest("toDate must be a valid date in yyyy-MM-dd format (e.g., 2026-01-31).");

                if (toDt < fromDt)
                    return BadRequest("toDate cannot be earlier than fromDate.");
                if (string.IsNullOrWhiteSpace(compId))
                    return BadRequest("compId is required.");

                string glCodeValue = glCode?.Trim() ?? "";
                var data = _dal.GetChequeDetailsExpRegionModel(compId.Trim(), glCodeValue, fromDt, toDt);

                var totalDrAmt = data.Sum(x => x.DrAmt ?? 0m);

                const int MAX_RECORDS = 50000;
                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy-MM-dd"),
                    toDate = toDt.ToString("yyyy-MM-dd"),
                    compId = compId.Trim(),
                    glCode = glCodeValue,
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

        private static string GetConnectionString()
        {
            var connectionStringSetting = ConfigurationManager.ConnectionStrings["OracleDb"];
            if (connectionStringSetting != null && !string.IsNullOrWhiteSpace(connectionStringSetting.ConnectionString))
                return connectionStringSetting.ConnectionString;

            string[] fallbackNames = { "HQOracle", "OracleTest", "THQOracle", "wareHQOracle" };
            foreach (string name in fallbackNames)
            {
                var fallbackSetting = ConfigurationManager.ConnectionStrings[name];
                if (fallbackSetting != null && !string.IsNullOrWhiteSpace(fallbackSetting.ConnectionString))
                    return fallbackSetting.ConnectionString;
            }

            throw new InvalidOperationException("No Oracle connection string is configured. Please set 'OracleDb' in web.config.");
        }
    }
}