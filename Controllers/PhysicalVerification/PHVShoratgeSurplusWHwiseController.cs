using MISReports_Api.Compat;
using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-shortage-surplus-whwise")]
    public class PHVShortageSurplusWHwiseController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PHVShortageSurplusWHwiseRepository _repository;

        public PHVShortageSurplusWHwiseController()
        {
            _repository = new PHVShortageSurplusWHwiseRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetShortageSurplusWHwise(
            string deptId,
            int repYear,
            int repMonth,
            string warehouseCode)
        {
            if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                return BadRequest("Department Id and Warehouse Code are required.");

            try
            {
                var data = await _repository.GetShortageSurplusWHwiseAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear,
                    repMonth
                );

                if (data.Count == 0)
                    return Ok(new { message = "No data found." });

                return Ok(data);
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException ex)
            {
                logger.Error(ex, "GetShortageSurplusWHwise failed. deptId={deptId}, repYear={repYear}, repMonth={repMonth}, warehouseCode={warehouseCode}", deptId, repYear, repMonth, warehouseCode);
                if (ex.Number == 1652)
                    return InternalServerError(new Exception("Database TEMP tablespace full. Consider optimizing the query or increasing TEMP tablespace."));

                return InternalServerError(ex);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetShortageSurplusWHwise failed. deptId={deptId}, repYear={repYear}, repMonth={repMonth}, warehouseCode={warehouseCode}", deptId, repYear, repMonth, warehouseCode);
                return InternalServerError(ex);
            }
        }
    }
}
