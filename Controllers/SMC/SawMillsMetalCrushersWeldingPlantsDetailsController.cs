
using MISReports_Api.Compat;
using MISReports_Api.DAL.SMC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers.SMC
{
    [RoutePrefix("api/saw-mills-metal-crushers-welding-plants-details")]
    public class SawMillsMetalCrushersWeldingPlantsDetailsController
        : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly SawMillsMetalCrushersWeldingPlantsDetailsDAL _dal =
            new SawMillsMetalCrushersWeldingPlantsDetailsDAL();

        [HttpGet]
        [Route("report")]
        public IActionResult GetReport(
            [FromUri] string fromDate,
            [FromUri] string toDate)
        {
            if (string.IsNullOrWhiteSpace(fromDate))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "From date is required."
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }

            if (string.IsNullOrWhiteSpace(toDate))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "To date is required."
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }

            string[] formats =
            {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyyMMdd"
            };

            if (!DateTime.TryParseExact(
                    fromDate.Trim(),
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fromDt))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage =
                        "Invalid fromDate format. Use yyyy-MM-dd."
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }

            if (!DateTime.TryParseExact(
                    toDate.Trim(),
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime toDt))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage =
                        "Invalid toDate format. Use yyyy-MM-dd."
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }

            if (toDt.Date < fromDt.Date)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage =
                        "To date cannot be earlier than from date."
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }

            try
            {
                var reportData =
                    _dal.GetSawMillsMetalCrushersWeldingPlantsDetails(
                        fromDt,
                        toDt);

                var response = new
                {
                    data = reportData,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetReport failed. fromDate={fromDate}, toDate={toDate}", fromDate, toDate);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage =
                        "Cannot get Saw Mills, Metal Crushers and Welding Plants details.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
