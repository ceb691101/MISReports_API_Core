using MISReports_Api.DAL;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercard/current-account-reconciliation-external")]
    public class CurrentAccountReconciliationExternalController : ApiController
    {
        private readonly CurrentAccountReconciliationExternalRepository _repository;

        public CurrentAccountReconciliationExternalController()
        {
            _repository = new CurrentAccountReconciliationExternalRepository();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetReport(
            [FromUri] string compId, 
            [FromUri] int repyear, 
            [FromUri] int repmonth, 
            [FromUri] string subac)
        {
            try
            {
                if (string.IsNullOrEmpty(compId) || string.IsNullOrEmpty(subac))
                {
                    return BadRequest("Parameters 'compId' and 'subac' are required.");
                }

                var data = _repository.GetData(compId, repyear, repmonth, subac);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
