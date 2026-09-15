using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-damage")]
    public class PHVDamageController : ApiController
    {
        private readonly PHVDamageRepository _repository;

        public PHVDamageController()
        {
            _repository = new PHVDamageRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetDamage(
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
                return InternalServerError(ex);
            }
        }
    }
}
