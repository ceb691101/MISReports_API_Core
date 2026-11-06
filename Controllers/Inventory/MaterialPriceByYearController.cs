using MISReports_Api.Compat;
using MISReports_Api.DAL.Inventory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/material-price")]
    public class MaterialPriceByYearController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly MaterialPriceByYearDAL _materialPriceByYearDAL =
            new MaterialPriceByYearDAL();

        [HttpGet]
        [Route("")]
        public IActionResult GetMaterialPriceByYear(
            [FromUri] string costCtr,
            [FromUri] string repYear)
        {
            if (string.IsNullOrWhiteSpace(costCtr))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cost center is required."
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }

            if (string.IsNullOrWhiteSpace(repYear))
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Report year is required."
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }

            try
            {
                var materialPrices =
                    _materialPriceByYearDAL.GetMaterialPriceByYear(
                        costCtr,
                        repYear);

                var response = new
                {
                    data = materialPrices,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetMaterialPriceByYear failed. costCtr={costCtr}, repYear={repYear}", costCtr, repYear);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get material price details.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(
                    JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}