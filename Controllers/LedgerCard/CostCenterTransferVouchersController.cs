using MISReports_Api.DAL;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercard/costcenter-transfer-vouchers")]
    public class CostCenterTransferVouchersController : ApiController
    {
        private readonly CostCenterTransferVouchersRepository _repository;

        public CostCenterTransferVouchersController()
        {
            _repository = new CostCenterTransferVouchersRepository();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetReport(
            [FromUri] string costctr, 
            [FromUri] int repyear, 
            [FromUri] int startmonth, 
            [FromUri] int endmonth, 
            [FromUri] string subac,
            [FromUri] string docpf = null)
        {
            try
            {
                if (string.IsNullOrEmpty(costctr) || string.IsNullOrEmpty(subac))
                {
                    return BadRequest("Parameters 'costctr' and 'subac' are required.");
                }

                var data = _repository.GetCostCenterTransferVouchersData(costctr, repyear, startmonth, endmonth, subac, docpf);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("doc-profiles")]
        public IHttpActionResult GetDocProfiles([FromUri] string costctr)
        {
            try
            {
                if (string.IsNullOrEmpty(costctr))
                {
                    return BadRequest("Parameter 'costctr' is required.");
                }

                var data = _repository.GetDocProfiles(costctr);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
