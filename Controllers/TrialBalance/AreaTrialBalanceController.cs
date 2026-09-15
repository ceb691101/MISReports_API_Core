using MISReports_Api.DAL;
using MISReports_Api.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers.TrialBalance
{
    [RoutePrefix("api/areatrialbalance")]
    public class AreaTrialBalanceController : ApiController
    {
        private readonly AreaTrialBalanceRepository _repository;

        public AreaTrialBalanceController()
        {
            _repository = new AreaTrialBalanceRepository();
        }

        [HttpGet]
        [Route("list")]
        public IHttpActionResult GetAreaTrialBalanceList(
            [FromUri] string companyId,
            [FromUri] int year,
            [FromUri] int month)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                    return BadRequest("companyId is required.");
                if (year <= 0)
                    return BadRequest("year is required.");
                if (month <= 0 || month > 12)
                    return BadRequest("month must be between 1 and 12.");

                var data = _repository.GetAreaTrialBalanceData(companyId.Trim(), year, month);

                return Json(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("pdf")]
        public HttpResponseMessage GetAreaTrialBalancePdf(
            [FromUri] string companyId,
            [FromUri] int year,
            [FromUri] int month,
            [FromUri] bool download = false)
        {
            return Request.CreateResponse(HttpStatusCode.NotImplemented, new
            {
                success = false,
                message = "JasperSoft reporting has been decommissioned. Please consume the /list endpoint for trial balance records."
            });
        }

        [HttpGet]
        [Route("csv")]
        public HttpResponseMessage GetAreaTrialBalanceCsv(
            [FromUri] string companyId,
            [FromUri] int year,
            [FromUri] int month)
        {
            return Request.CreateResponse(HttpStatusCode.NotImplemented, new
            {
                success = false,
                message = "JasperSoft reporting has been decommissioned. Please consume the /list endpoint for trial balance records."
            });
        }
    }
}
