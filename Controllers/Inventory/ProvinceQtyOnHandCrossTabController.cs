using MISReports_Api.Compat;
using MISReports_Api.DAL.Inventory;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers.Inventory
{
    [RoutePrefix("api/province-qty-onhand-crosstab")]
    public class ProvinceQtyOnHandCrossTabController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ProvinceQtyOnHandCrossTabRepository _repository;

        public ProvinceQtyOnHandCrossTabController()
        {
            _repository = new ProvinceQtyOnHandCrossTabRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<HttpResponseMessage> GetReport(
            string compId,
            string matcode)
        {
            try
            {
                var data = await _repository.GetReportAsync(compId, matcode);

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
                logger.Error(ex, "GetReport failed. compId={compId}, matcode={matcode}", compId, matcode);
                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { message = ex.Message },
                    Configuration.Formatters.JsonFormatter);
            }
        }
    }
}