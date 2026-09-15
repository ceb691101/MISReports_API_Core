using MISReports_Api.DAL.BillMap;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/billmap")]
    public class BillMapController : ApiController
    {
        private readonly BillMapRepository _repository;

        public BillMapController()
        {
            _repository = new BillMapRepository();
        }

        [HttpGet]
        [Route("{epfNo}")]
        public async Task<IHttpActionResult> GetBillMaps(string epfNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(epfNo))
                    return BadRequest("EPF Number is required.");

                var result = await _repository.GetBillMapsAsync(epfNo.Trim());

                return Ok(result);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}