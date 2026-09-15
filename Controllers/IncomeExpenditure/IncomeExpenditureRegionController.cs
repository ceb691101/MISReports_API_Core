using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/incomeexpenditureregion")]
    public class IncomeExpenditureRegionController : ApiController
    {
        private readonly IncomeExpenditureRegionRepository _repository = new IncomeExpenditureRegionRepository();

        [HttpGet]
        [Route("{companyId}/{repyear:int}/{repmonth:int}")]
        public IHttpActionResult GetIncomeExpenditureRegion(string companyId, int repyear, int repmonth)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                    throw new ArgumentException("Company ID is required.");

                if (repyear < 1900 || repyear > 2100)
                    throw new ArgumentException("Invalid year.");

                if (repmonth < 1 || repmonth > 12)
                    throw new ArgumentException("Invalid month.");

                var result = _repository.GetIncomeExpenditureRegion(companyId.Trim(), repyear, repmonth);

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (ArgumentException aex)
            {
                var error = new
                {
                    data = (object)null,
                    errorMessage = aex.Message,
                    errorDetails = (string)null
                };
                return BadRequest(JsonConvert.SerializeObject(error));
            }
            catch (Exception ex)
            {
                var error = new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve Income vs Expenditure Region data.",
                    errorDetails = ex.Message
                };
                return Ok(JObject.Parse(JsonConvert.SerializeObject(error)));
            }
        }
    }
}