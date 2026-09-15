using MISReports_Api.DAL;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercard/current-account-reconciliation-own-other")]
    public class CurrAcctReconOwnOtherController : ApiController
    {
        private readonly CurrAcctReconOwnOtherRepository _repository;

        public CurrAcctReconOwnOtherController()
        {
            _repository = new CurrAcctReconOwnOtherRepository();
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetReport(
            [FromUri(Name = "REGION")] string region,
            [FromUri(Name = "YEAR")] int year,
            [FromUri(Name = "MONTH")] int month,
            [FromUri(Name = "SUBAC")] string subac)
        {
            try
            {
                if (string.IsNullOrEmpty(region) || string.IsNullOrEmpty(subac))
                {
                    return BadRequest("Parameters 'REGION' and 'SUBAC' are required.");
                }

                var data = _repository.GetData(region, year, month, subac);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
