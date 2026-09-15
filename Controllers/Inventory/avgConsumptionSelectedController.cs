using MISReports_Api.DAL;
using MISReports_Api.Models;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/avg-consumption-selected")]
    public class AvgConsumptionSelectedController : ApiController
    {
        private readonly IAvgConsumptionSelectedDataRepository _repository;

        public AvgConsumptionSelectedController()
        {
            _repository = new AvgConsumptionSelectedDataRepository();
        }

        [HttpGet]
        [Route("{costCenter}/{warehouseCode}/{fromDate}/{toDate}/{matCode?}")]
        public IHttpActionResult GetSelectedAverageConsumption(
            string costCenter,
            string warehouseCode,
            string fromDate,
            string toDate,
            string matCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCenter) || string.IsNullOrWhiteSpace(warehouseCode))
                    return BadRequest("Cost Center and Warehouse Code are required");

                if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFrom) ||
                    !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedTo))
                {
                    return BadRequest("Invalid date format. Use yyyyMMdd (example: 20250101)");
                }

                var data = _repository.GetSelectedAverageConsumption(
                    costCenter.Trim(),
                    warehouseCode.Trim(),
                    parsedFrom.Date,
                    parsedTo.Date,
                    string.IsNullOrWhiteSpace(matCode) ? null : matCode.Trim()
                );

                return Ok(new AvgConsumptionSelectedResponse
                {
                    Data = data,
                    ErrorMessage = null,
                    ErrorDetails = null
                });
            }
            catch (Exception ex)
            {
                return Ok(new AvgConsumptionSelectedResponse
                {
                    Data = null,
                    ErrorMessage = "Failed to retrieve selected material average consumption data",
                    ErrorDetails = ex.Message
                });
            }
        }
    }
}