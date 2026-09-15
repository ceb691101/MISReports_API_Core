using MISReports_Api.DAL;
using MISReports_Api.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/inventoryaverageconsumption")]
    public class InventoryAverageConsumptionController : ApiController
    {
        private readonly InventoryAverageConsumptionRepository _repository = new InventoryAverageConsumptionRepository();

        // ---------------------- REPORT ENDPOINT ----------------------
        [HttpGet]
        [Route("report/{costCenter}/{warehouseCode}/{fromDate}/{toDate}")]
        public IHttpActionResult GetAverageConsumption(string costCenter, string warehouseCode, string fromDate, string toDate)
        {
            try
            {
                // Validate parameters
                if (string.IsNullOrWhiteSpace(costCenter) || string.IsNullOrWhiteSpace(warehouseCode))
                {
                    return BadRequest("Cost center and warehouse code cannot be empty");
                }

                // Validate dates - Input format: yyyyMMdd
                if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedFromDate) ||
                    !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedToDate))
                {
                    return BadRequest("Invalid date format. Please use yyyyMMdd format");
                }

                var result = _repository.GetAverageConsumption(
                    costCenter.Trim(),
                    warehouseCode.Trim(),
                    parsedFromDate.Date,
                    parsedToDate.Date);

                return Ok(new InventoryAverageConsumptionResponse
                {
                    Data = result,
                    ErrorMessage = null,
                    ErrorDetails = null
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAverageConsumption: {ex}");
                return Ok(new InventoryAverageConsumptionResponse
                {
                    Data = null,
                    ErrorMessage = "Cannot get average consumption data.",
                    ErrorDetails = ex.Message
                });
            }
        }

        // ---------------------- WAREHOUSE ENDPOINT ----------------------
        [HttpGet]
        [Route("warehouses/{epfNo}")]
        public IHttpActionResult GetWarehousesByEpfNo(string epfNo, [FromUri] string costCenterId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(epfNo))
                {
                    return BadRequest("EPF number cannot be empty");
                }

                var warehouses = _repository.GetWarehousesByEpfNoAndCostCenter(epfNo, costCenterId);

                return Ok(new WarehouseResponse
                {
                    Data = warehouses,
                    ErrorMessage = null,
                    ErrorDetails = null
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetWarehousesByEpfNo: {ex}");
                return Ok(new WarehouseResponse
                {
                    Data = null,
                    ErrorMessage = "Cannot get warehouse data.",
                    ErrorDetails = ex.Message
                });
            }
        }
    }
}