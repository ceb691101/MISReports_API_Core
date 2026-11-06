using MISReports_Api.Compat;
using MISReports_Api.DAL;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercard/current-account-reconciliation-external")]
    public class CurrentAccountReconciliationExternalController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly CurrentAccountReconciliationExternalRepository _repository;

        public CurrentAccountReconciliationExternalController()
        {
            _repository = new CurrentAccountReconciliationExternalRepository();
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetReport(
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
                logger.Error(ex, "GetReport failed. compId={compId}, repyear={repyear}, repmonth={repmonth}, subac={subac}", compId, repyear, repmonth, subac);
                return InternalServerError(ex);
            }
        }
    }
}
