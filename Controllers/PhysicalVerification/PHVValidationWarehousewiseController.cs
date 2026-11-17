using MISReports_Api.Compat;
using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-validation-warehousewise")]
    public class PHVValidationWarehousewiseController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PHVValidationWarehousewiseRepository _repository;

        public PHVValidationWarehousewiseController()
        {
            _repository = new PHVValidationWarehousewiseRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetWarehousewiseValidation(
            string deptId,
            string warehouseCode,
            int repYear,   
            int repMonth) 
        {
            try
            {
                var data = await _repository.GetWarehousewiseValidationAsync(
                    deptId,
                    warehouseCode,
                    repYear,
                    repMonth);

                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetWarehousewiseValidation failed. deptId={deptId}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}", deptId, warehouseCode, repYear, repMonth);
                return InternalServerError(ex);
            }
        }
    }
}