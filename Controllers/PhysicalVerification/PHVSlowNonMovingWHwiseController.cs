using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-slow-nonmoving-whwise")]
    public class PHVSlowNonMovingWHwiseController : ApiController
    {
        private readonly PHVSlowNonMovingWHwiseRepository _repository;

        public PHVSlowNonMovingWHwiseController()
        {
            _repository = new PHVSlowNonMovingWHwiseRepository();
        }

      
        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetSlowNonMovingWHwise(
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
                return InternalServerError(ex);
            }
        }
    }
}