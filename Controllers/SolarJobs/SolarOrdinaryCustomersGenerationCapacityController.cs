using MISReports_Api.Compat;
using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL.SolarJobs;

namespace MISReports_Api.Controllers.SolarJobs
{
    [RoutePrefix("api/solarordinarycustomersgenerationcapacity")]
    public class SolarOrdinaryCustomersGenerationCapacityController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly SolarOrdinaryCustomersGenerationCapacityDAL _dal =
            new SolarOrdinaryCustomersGenerationCapacityDAL();

        private static readonly string[] DateFormats =
        {
            "yyyy/MM/dd",
            "yyyy-MM-dd"
        };

        

        [HttpGet]
        [Route("report")]
        public IActionResult GetQuery(
            [FromUri] string fromDate,
            [FromUri] string toDate)
        {
            try
            {
                DateTime fromDt;
                DateTime toDt;

                if (string.IsNullOrWhiteSpace(fromDate) ||
                    !DateTime.TryParseExact(
                        fromDate.Trim(),
                        DateFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out fromDt))
                {
                    return BadRequest(
                        "fromDate must be a valid date in yyyy/MM/dd format.");
                }

                if (string.IsNullOrWhiteSpace(toDate) ||
                    !DateTime.TryParseExact(
                        toDate.Trim(),
                        DateFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out toDt))
                {
                    return BadRequest(
                        "toDate must be a valid date in yyyy/MM/dd format.");
                }

                if (toDt.Date < fromDt.Date)
                {
                    return BadRequest(
                        "toDate cannot be earlier than fromDate.");
                }

                var data =
                    _dal.GetSolarOrdinaryCustomersGenerationCapacity(
                        fromDt,
                        toDt);

                var summary = new
                {
                    fromDate = fromDt.ToString("yyyy/MM/dd"),
                    toDate = toDt.ToString("yyyy/MM/dd"),
                    totalAccounts = data.Sum(x => x.NoOfAccounts),
                    totalGeneratedCapacity =
                        data.Sum(x => x.GeneratedCapacity ?? 0)
                };

                return Ok(new
                {
                    success = true,
                    message = data.Any()
                        ? "Data retrieved successfully"
                        : "No records found",
                    data = data,
                    summary = summary
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetQuery failed. fromDate={fromDate}, toDate={toDate}", fromDate, toDate);
                System.Diagnostics.Debug.WriteLine(
                    "Error: " + ex.Message + "\n" + ex.StackTrace);

                return InternalServerError(
                    new Exception(
                        "Database error: " + ex.Message,
                        ex));
            }
        }
    }
}