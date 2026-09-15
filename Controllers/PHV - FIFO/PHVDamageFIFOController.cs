using MISReports_Api.DAL.FIFO;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers.FIFO
{
    [RoutePrefix("api/phv-damage-fifo")]
    public class PHVDamageFIFOController : ApiController
    {
        private readonly PHVDamageFIFORepository _repository;

        public PHVDamageFIFOController()
        {
            _repository = new PHVDamageFIFORepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetPHVDamageFIFO(
            string deptId,
            string warehouseCode,
            int repYear)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                    return BadRequest("deptId and warehouseCode are required.");

                var data = await _repository.GetPHVDamageFIFOAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear);

                return Json(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("pdf")]
        public HttpResponseMessage GetPHVDamageFIFOPdf(
            string deptId,
            string deptName,
            string warehouseCode,
            int repYear,
            int repMonth,
            bool download = false)
        {
            return Request.CreateResponse(HttpStatusCode.NotImplemented, new
            {
                success = false,
                message = "JasperSoft reporting has been decommissioned. Please consume the /list endpoint for FIFO records."
            });
        }

        [HttpGet]
        [Route("csv")]
        public HttpResponseMessage GetPHVDamageFIFOCsv(
            string deptId,
            string deptName,
            string warehouseCode,
            int repYear,
            int repMonth)
        {
            return Request.CreateResponse(HttpStatusCode.NotImplemented, new
            {
                success = false,
                message = "JasperSoft reporting has been decommissioned. Please consume the /list endpoint for FIFO records."
            });
        }
    }
}
