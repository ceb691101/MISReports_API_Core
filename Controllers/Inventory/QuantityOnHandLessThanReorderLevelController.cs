using MISReports_Api.Compat;
using System;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL.Inventory;


namespace MISReports_Api.Controllers.Inventory
{
    [RoutePrefix("api/quantityonhandlessthanreorderlevel")]
    public class QuantityOnHandLessThanReorderLevelController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly QuantityOnHandLessThanReorderLevelDAL _dal =
            new QuantityOnHandLessThanReorderLevelDAL();

        [HttpGet]
        [Route("report")]
        public IActionResult GetQuery(
            [FromUri] string costCtr)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                {
                    return BadRequest(
                        "costCtr is required.");
                }

                var data =
                    _dal.GetQuantityOnHandLessThanReorderLevel(
                        costCtr.Trim());

                return Ok(new
                {
                    success = true,

                    message = data.Count > 0
                        ? "Data retrieved successfully"
                        : "No records found",

                    costCtr = costCtr.Trim(),

                    recordCount = data.Count,

                    data = data
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetQuery failed. costCtr={costCtr}", costCtr);
                System.Diagnostics.Debug.WriteLine(
                    "Error: " +
                    ex.Message +
                    "\n" +
                    ex.StackTrace);

                return InternalServerError(
                    new Exception(
                        "Database error: " +
                        ex.Message,
                        ex));
            }
        }
    }
}