using MISReports_Api.Compat;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/trialbalance")]
    public class TrialBalanceController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly TrialBalanceRepository _repository = new TrialBalanceRepository();
   // one  company details 
        [HttpGet]
        [Route("{costctr}/{repyear}/{repmonth}")]
        public IActionResult GetTrialBalance(string costctr, string repyear, string repmonth)
        {
            try
            {
                var result = _repository.GetTrialBalance(costctr.Trim(), repyear.Trim(), repmonth.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetTrialBalance failed. costctr={costctr}, repyear={repyear}, repmonth={repmonth}", costctr, repyear, repmonth);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get trial balance data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
        //get department  reagion wise
        [HttpGet]
        [Route("departments/{region}")]
        public IActionResult GetDepartmentsByRegion(string region)
        {
            try
            {
                var result = _repository.GetDepartmentsByRegion(region.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetDepartmentsByRegion failed. region={region}", region);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get department data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        //get companys level vise
        [HttpGet]
        [Route("companies/level/{lvl_no}")]
        public IActionResult GetCompaniesByLevel(string lvl_no)
        {
            try
            {
                var result = _repository.GetCompaniesByLevel(lvl_no.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetCompaniesByLevel failed. lvl_no={lvl_no}", lvl_no);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get company list.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}
