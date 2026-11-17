using MISReports_Api.Compat;
using MISReports_Api.DAL;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercard/current-account-reconciliation-internal")]
    public class CurrAcctReconIntController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly CurrAcctReconIntRepository _repository;

        public CurrAcctReconIntController()
        {
            _repository = new CurrAcctReconIntRepository();
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetReport(
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
                logger.Error(ex, "GetReport failed. region={region}, year={year}, month={month}, subac={subac}", region, year, month, subac);
                return InternalServerError(ex);
            }
        }
    }
}
