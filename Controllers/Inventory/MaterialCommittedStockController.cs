using MISReports_Api.Compat;
using MISReports_Api.DAL;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/materialcommittedstock")]
    public class MaterialCommittedStockController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly MaterialCommittedStockRepository _repository =
            new MaterialCommittedStockRepository();

        // GET api/materialcommittedstock/provinces
        [HttpGet]
        [Route("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            try
            {
                var result = await _repository.GetProvinces();

                return Ok(new
                {
                    data = result,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetProvinces failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get provinces list.",
                    errorDetails = ex.Message
                });
            }
        }

        // GET api/materialcommittedstock/get?compId=EP&matCode=A
        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetMaterialCommittedStock(string compId, string matCode = null)
        {
            if (string.IsNullOrWhiteSpace(compId))
                return BadRequest("compId is required.");

            try
            {
                var result = await _repository.GetMaterialCommittedStock(compId.Trim(), matCode?.Trim());

                return Ok(new
                {
                    data = result,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetMaterialCommittedStock failed. compId={compId}, matCode={matCode}", compId, matCode);
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Material Committed Stock data.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}