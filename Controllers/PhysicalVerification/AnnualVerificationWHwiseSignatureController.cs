using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/annual-verification-whwise-signature")]
    public class AnnualVerificationWHwiseSignatureController : ApiController
    {
        private readonly AnnualVerificationWHwiseSignatureRepository _repository;

        public AnnualVerificationWHwiseSignatureController()
        {
            _repository = new AnnualVerificationWHwiseSignatureRepository();
        }

        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetSignatureData(
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
