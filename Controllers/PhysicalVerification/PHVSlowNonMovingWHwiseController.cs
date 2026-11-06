using MISReports_Api.Compat;
using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-slow-nonmoving-whwise")]
    public class PHVSlowNonMovingWHwiseController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PHVSlowNonMovingWHwiseRepository _repository;

        public PHVSlowNonMovingWHwiseController()
        {
            _repository = new PHVSlowNonMovingWHwiseRepository();
        }

      
        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetSlowNonMovingWHwise(
            string deptId,
            int repYear,
            int repMonth,
            string warehouseCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) ||
                    string.IsNullOrWhiteSpace(warehouseCode))
                {
                    return BadRequest("Department Id and Warehouse Code are required.");
                }

                var data = await _repository.GetSlowNonMovingWHwiseAsync(
                    deptId.Trim(),
                    repYear,
                    repMonth,
                    warehouseCode.Trim());

                return Json(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetSlowNonMovingWHwise failed. deptId={deptId}, repYear={repYear}, repMonth={repMonth}, warehouseCode={warehouseCode}", deptId, repYear, repMonth, warehouseCode);
                return InternalServerError(ex);
            }
        }
    }
}