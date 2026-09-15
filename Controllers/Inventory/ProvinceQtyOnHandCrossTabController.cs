using MISReports_Api.DAL.Inventory;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers.Inventory
{
    [RoutePrefix("api/province-qty-onhand-crosstab")]
    public class ProvinceQtyOnHandCrossTabController : ApiController
    {
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
                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { message = ex.Message },
                    Configuration.Formatters.JsonFormatter);
            }
        }
    }
}