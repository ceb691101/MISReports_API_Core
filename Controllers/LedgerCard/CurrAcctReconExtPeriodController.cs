using MISReports_Api.DAL;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercard/curr-acct-recon-ext-period")]
    public class CurrAcctReconExtPeriodController : ApiController
    {
        private readonly CurrAcctReconExtPeriodRepository _repository;

        public CurrAcctReconExtPeriodController()
        {
            _repository = new CurrAcctReconExtPeriodRepository();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetReport(
            [FromUri] string compId, 
            [FromUri] int repyear, 
            [FromUri] int monthFrom, 
            [FromUri] int monthTo, 
            [FromUri] string subac)
        {
            try
            {
                if (string.IsNullOrEmpty(compId) || string.IsNullOrEmpty(subac))
                {
                    return BadRequest("Parameters 'compId' and 'subac' are required.");
                }

                if (monthFrom < 1 || monthFrom > 12 || monthTo < 1 || monthTo > 12)
                {
                    return BadRequest("Month values must be between 1 and 12.");
                }

                var data = _repository.GetData(compId, repyear, monthFrom, monthTo, subac);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
