using MISReports_Api.Compat;
using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-damage")]
    public class PHVDamageController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PHVDamageRepository _repository;

        public PHVDamageController()
        {
            _repository = new PHVDamageRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetDamage(
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

                var data = await _repository.GetDamageAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear,
                    repMonth);

                return Json(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetDamage failed. deptId={deptId}, repYear={repYear}, repMonth={repMonth}, warehouseCode={warehouseCode}", deptId, repYear, repMonth, warehouseCode);
                return InternalServerError(ex);
            }
        }
    }
}
