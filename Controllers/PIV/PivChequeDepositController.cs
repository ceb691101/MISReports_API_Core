// 11. PIV Details for Cheque Deposits/ Cheque No

// File: PivChequeDepositController.cs
using MISReports_Api.Compat;
using MISReports_Api.DAL.PIV;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/pivchequedeposit")]
    public class PivChequeDepositController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PivChequeDepositRepository _repo = new PivChequeDepositRepository();

        // GET: api/pivchequedeposit/get?chequeNo=123456
        [HttpGet]
        [Route("get")]
        public IActionResult Get(string chequeNo)
        {
            if (string.IsNullOrWhiteSpace(chequeNo))
            {
                return BadRequest("chequeNo is required.");
            }

            try
            {
                var data = _repo.GetApplicationsByChequeNo(chequeNo);
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Get failed. chequeNo={chequeNo}", chequeNo);
                return InternalServerError(new Exception("Error while fetching cheque deposit details: " + ex.Message));
            }
        }
    }
}