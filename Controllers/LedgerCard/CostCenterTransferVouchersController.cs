using MISReports_Api.Compat;
using MISReports_Api.DAL;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/ledgercard/costcenter-transfer-vouchers")]
    public class CostCenterTransferVouchersController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly CostCenterTransferVouchersRepository _repository;

        public CostCenterTransferVouchersController()
        {
            _repository = new CostCenterTransferVouchersRepository();
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetReport(
            [FromUri] string costctr,
            [FromUri] int repyear,
            [FromUri] int startmonth,
            [FromUri] int endmonth,
            [FromUri] string subac = null,
            [FromUri] string docpf = null)
        {
            try
            {
                if (string.IsNullOrEmpty(costctr))
                {
                    return BadRequest("Parameter 'costctr' is required.");
                }

                var data = _repository.GetCostCenterTransferVouchersData(costctr, repyear, startmonth, endmonth, subac, docpf);
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetReport failed. costctr={costctr}, repyear={repyear}, startmonth={startmonth}, endmonth={endmonth}, subac={subac}, docpf={docpf}", costctr, repyear, startmonth, endmonth, subac, docpf);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("doc-profiles")]
        public IActionResult GetDocProfiles([FromUri] string costctr)
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
                logger.Error(ex, "GetDocProfiles failed. costctr={costctr}", costctr);
                return InternalServerError(ex);
            }
        }
    }
}