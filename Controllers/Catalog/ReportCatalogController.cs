using MISReports_Api.Compat;
using MISReports_Api.DAL.Catalog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers.Catalog
{
    [RoutePrefix("api/reportcatalog")]
    public class ReportCatalogController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ReportCatalogRepository _repository = new ReportCatalogRepository();

        // GET api/reportcatalog/all?epfNo=12345&roleId=ROLE_ADMIN
        [HttpGet]
        [Route("all")]
        public async Task<IActionResult> GetAllReports(string epfNo = null, string roleId = null)
        {
            try
            {
                var result = await _repository.GetAllCatalogReportsAsync(epfNo, roleId);

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAllReports failed. epfNo={epfNo}, roleId={roleId}", epfNo, roleId);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Failed to load report catalog.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
