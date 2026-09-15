using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-obsolete-idle")]
    public class PHVObsoleteIdleController : ApiController
    {
        private readonly PHVObsoleteIdleRepository _repository;

        public PHVObsoleteIdleController()
        {
            _repository = new PHVObsoleteIdleRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetObsoleteIdle(
            string deptId,
            int repYear,
            int repMonth,
            string warehouseCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                    return BadRequest("Department Id and Warehouse Code are required.");

                var data = await _repository.GetObsoleteIdleAsync(
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

        [HttpGet]
        [Route("pdf")]
        public HttpResponseMessage GetObsoleteIdlePdf(
            string deptId,
            string deptName,
            int repYear,
            int repMonth,
            string warehouseCode,
            bool download = false)
        {
            return Request.CreateResponse(HttpStatusCode.NotImplemented, new
            {
                success = false,
                message = "JasperSoft reporting has been decommissioned. Please consume the /list endpoint for obsolete/idle records."
            });
        }
    }
}
