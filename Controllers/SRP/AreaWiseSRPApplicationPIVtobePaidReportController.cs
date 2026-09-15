using MISReports_Api.DAL.PIV;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers.PIV
{
    [RoutePrefix("api/area-wise-srp-piv-pending")]
    public class AreaWiseSRPApplicationPIVtobePaidReportController : ApiController
    {
        private readonly AreaWiseSRPApplicationPIVtobePaidReportRepository _repository;

        public AreaWiseSRPApplicationPIVtobePaidReportController()
        {
            _repository = new AreaWiseSRPApplicationPIVtobePaidReportRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<HttpResponseMessage> GetReport(
            string compId,
            string fromDate,
            string toDate)
        {
            try
            {
                var data = await _repository.GetReportAsync(compId, fromDate, toDate);

                if (data.Count == 0)
                {
                    return Request.CreateResponse(
                        HttpStatusCode.OK,
                        new { message = "No data found." },
                        Configuration.Formatters.JsonFormatter);
                }

                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    data,
                    Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { message = ex.Message },
                    Configuration.Formatters.JsonFormatter);
            }
        }
    }
}