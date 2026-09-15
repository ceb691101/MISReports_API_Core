// 11. PIV Details for Cheque Deposits/ Cheque No

// File: PivChequeDepositController.cs
using MISReports_Api.DAL.PIV;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/pivchequedeposit")]
    public class PivChequeDepositController : ApiController
    {
        private readonly PivChequeDepositRepository _repo = new PivChequeDepositRepository();

        // GET: api/pivchequedeposit/get?chequeNo=123456
        [HttpGet]
        [Route("get")]
        public IHttpActionResult Get(string chequeNo)
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
                return InternalServerError(new Exception("Error while fetching cheque deposit details: " + ex.Message));
            }
        }
    }
}