using MISReports_Api.DAL;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/last-doc")]
    public class LastDocController : ApiController
    {
        private readonly LastDocRepository _repository;

        public LastDocController()
        {
            _repository = new LastDocRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<HttpResponseMessage> GetLastDoc(string deptId, int repYear)
        {
            if (string.IsNullOrWhiteSpace(deptId))
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    new { message = "Department Id is required." },
                    Configuration.Formatters.JsonFormatter);
            }

            try
            {
                var data = await _repository.GetLastDocAsync(deptId.Trim(), repYear);

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