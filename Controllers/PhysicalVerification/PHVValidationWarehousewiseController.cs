using MISReports_Api.DAL.PhysicalVerification;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-validation-warehousewise")]
    public class PHVValidationWarehousewiseController : ApiController
    {
        private readonly PHVValidationWarehousewiseRepository _repository;

        public PHVValidationWarehousewiseController()
        {
            _repository = new PHVValidationWarehousewiseRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetWarehousewiseValidation(
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
                return InternalServerError(ex);
            }
        }
    }
}