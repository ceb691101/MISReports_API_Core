using MISReports_Api.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/inventoryonhand")]
    public class InventoryOnHandController : ApiController
    {
        private readonly InventoryOnHandRepository _repository = new InventoryOnHandRepository();

        [HttpGet]
        [Route("{deptId}")]
        public async Task<IHttpActionResult> GetInventoryOnHand(string deptId, string matCode = null)
        {
            Debug.WriteLine($"API Request received for InventoryOnHand deptId: {deptId}, matCode: {matCode}");

            try
            {
                var result = await _repository.GetInventoryOnHand(deptId.Trim(), matCode?.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Inventory On Hand data.",
                    errorDetails = ex.Message,
                    stackTrace = ex.StackTrace
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}