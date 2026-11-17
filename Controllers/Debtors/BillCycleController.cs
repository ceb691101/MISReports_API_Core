// Controllers/BillCycleController.cs
using MISReports_Api.Compat;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/billcycle")]
    public class BillCycleController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly BillCycleRepository _repository = new BillCycleRepository();

        [HttpGet]
        [Route("max")]
        public IActionResult GetMaxBillCycle()
        {
            try
            {
                var result = _repository.GetLast24BillCycles();

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetMaxBillCycle failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("customerType/{custType}")]
        public IActionResult GetCustomerTypeDescription(string custType)
        {
            try
            {
                var result = _repository.GetCustomerTypeDescription(custType);

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetCustomerTypeDescription failed. custType={custType}", custType);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get customer type description",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}