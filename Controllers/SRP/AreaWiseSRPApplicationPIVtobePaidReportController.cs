using MISReports_Api.Compat;
using MISReports_Api.DAL.PIV;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers.PIV
{
    [RoutePrefix("api/area-wise-srp-piv-pending")]
    public class AreaWiseSRPApplicationPIVtobePaidReportController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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
                logger.Error(ex, "GetReport failed. compId={compId}, fromDate={fromDate}, toDate={toDate}", compId, fromDate, toDate);
                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { message = ex.Message },
                    Configuration.Formatters.JsonFormatter);
            }
        }
    }
}