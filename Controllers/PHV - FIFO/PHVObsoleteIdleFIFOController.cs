using MISReports_Api.DAL.FIFO;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers.FIFO
{
    [RoutePrefix("api/phv-obsolete-idle-fifo")]
    public class PHVObsoleteIdleFIFOController : ApiController
    {
        private readonly PHVObsoleteIdleFIFORepository _repository;

        public PHVObsoleteIdleFIFOController()
        {
            _repository = new PHVObsoleteIdleFIFORepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetPHVObsoleteIdleFIFO(
            string deptId,
            string warehouseCode,
            int repYear)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                    return BadRequest("deptId and warehouseCode are required.");

                var data = await _repository.GetPHVObsoleteIdleFIFOAsync(
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
        public HttpResponseMessage GetPHVObsoleteIdleFIFOPdf(
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
        public HttpResponseMessage GetPHVObsoleteIdleFIFOCsv(
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