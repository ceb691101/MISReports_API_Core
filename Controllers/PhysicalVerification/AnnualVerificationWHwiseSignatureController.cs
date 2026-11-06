using MISReports_Api.Compat;
using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/annual-verification-whwise-signature")]
    public class AnnualVerificationWHwiseSignatureController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AnnualVerificationWHwiseSignatureRepository _repository;

        public AnnualVerificationWHwiseSignatureController()
        {
            _repository = new AnnualVerificationWHwiseSignatureRepository();
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetSignatureData(
            string deptId,
            string warehouseCode,
            int repYear,
            int repMonth)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId))
                    return BadRequest("deptId is required.");

                if (string.IsNullOrWhiteSpace(warehouseCode))
                    return BadRequest("warehouseCode is required.");

                var result = await _repository
                    .GetAnnualVerificationWHwiseSignatureAsync(
                        deptId,
                        warehouseCode,
                        repYear,
                        repMonth);

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetSignatureData failed. deptId={deptId}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}", deptId, warehouseCode, repYear, repMonth);
                return Ok(new
                {
                    success = false,
                    message = "Error retrieving annual verification WH-wise signature data",
                    error = ex.Message
                });
            }
        }
    }
}
