using MISReports_Api.DAL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/divisionwise-srp-estimation")]
    public class DivisionWiseSRPEstimationController : ApiController
    {
        private readonly DivisionWiseSRPEstimationRepository _repository =
            new DivisionWiseSRPEstimationRepository();

        // GET api/divisionwise-srp-estimation/get?compId=NCP&fromDate=2025/01/01&toDate=2025/12/31
        [HttpGet]
        [Route("get")]
        public async Task<IHttpActionResult> GetDivisionWiseSRP(
            string compId,
            string fromDate,
            string toDate)
        {
            if (string.IsNullOrWhiteSpace(compId))
                return BadRequest("compId is required.");

            try
            {
                var result = await _repository.GetDivisionWiseSRP(
                    compId.Trim(),
                    fromDate,
                    toDate);

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
                    errorMessage = "Cannot get Division Wise SRP Estimation data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}