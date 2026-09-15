using System;
using MISReports_Api.DAL.PhysicalVerification;
using System.Threading.Tasks;
using System.Web.Http;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-obsolete-idle-bos")]
    public class PHVObsoleteIdleBOSController : ApiController
    {
        private readonly PHVObsoleteIdleBOSRepository _repository;

        public PHVObsoleteIdleBOSController()
        {
            _repository = new PHVObsoleteIdleBOSRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<HttpResponseMessage> GetObsoleteIdleBOS(
            string deptId,
            int repYear,
            int repMonth,
            string warehouseCode)
        {
            if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    new { message = "Department Id and Warehouse Code are required." },
                    Configuration.Formatters.JsonFormatter
                );
            }

            try
            {
                var data = await _repository.GetObsoleteIdleBOSAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear,
                    repMonth
                );

                if (data.Count == 0)
                {
                    return Request.CreateResponse(
                        HttpStatusCode.OK,
                        new { message = "No data found." },
                        Configuration.Formatters.JsonFormatter
                    );
                }

                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    data,
                    Configuration.Formatters.JsonFormatter
                );
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException ex)
            {
                if (ex.Number == 1652)
                {
                    return Request.CreateResponse(
                        HttpStatusCode.InternalServerError,
                        new { message = "Database TEMP tablespace full." },
                        Configuration.Formatters.JsonFormatter
                    );
                }

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { message = ex.Message },
                    Configuration.Formatters.JsonFormatter
                );
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    new { message = ex.Message },
                    Configuration.Formatters.JsonFormatter
                );
            }
        }
    }
}