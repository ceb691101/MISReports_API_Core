using MISReports_Api.Compat;
using MISReports_Api.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/warehouse")]
    public class WarehouseController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly WarehouseRepository _repository = new WarehouseRepository();

        // Endpoint: GET /api/warehouse/{epfNo}
        [HttpGet]
        [Route("{epfNo}")]
        public IActionResult GetWarehousesByEpf(string epfNo)
        {
            try
            {
                var result = _repository.GetWarehousesByEpf(epfNo.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetWarehousesByEpf failed. epfNo={epfNo}", epfNo);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Warehouses.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
