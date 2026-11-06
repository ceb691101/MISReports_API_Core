using MISReports_Api.Compat;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Mvc;


namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/incomeexpenditure")]
    public class IncomeExpenditureController : ApiController

    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IncomeExpenditureRepository _repository = new IncomeExpenditureRepository();
        // one  company details 
        [HttpGet]
        [Route("{costctr}/{repyear}/{repmonth}")]
        public IActionResult GetIncomeExpenditure(string costctr, string repyear, string repmonth)
        {
            try
            {
                var result = _repository.GetIncomeExpenditure(costctr.Trim(), repyear.Trim(), repmonth.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetIncomeExpenditure failed. costctr={costctr}, repyear={repyear}, repmonth={repmonth}", costctr, repyear, repmonth);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Income over Expenditure data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
        //get department  User wise
        [HttpGet]
        [Route("departments/{epfno}")]
        public IActionResult GetDepartmentsByUser(string epfno)
        {
            try
            {
                var result = _repository.GetDepartmentsByUser(epfno.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetDepartmentsByUser failed. epfno={epfno}", epfno);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get department data.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }

        //get companys user level wise
        [HttpGet]
        [Route("Usercompanies/{epfno}/{lvl_no}")]

        public IActionResult GetCompaniesByUserLevel(string epfno, string lvl_no)
        {
            try
            {
                var result = _repository.GetCompaniesByUserlevel(epfno.Trim(), lvl_no.Trim());

                var response = new
                {
                    data = result,
                    errorMessage = (string)null
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(response)));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetCompaniesByUserLevel failed. epfno={epfno}, lvl_no={lvl_no}", epfno, lvl_no);
                var errorResponse = new
                {
                    data = (object)null,
                    errorMessage = "Cannot get company list for the user.",
                    errorDetails = ex.Message
                };

                return Ok(JObject.Parse(JsonConvert.SerializeObject(errorResponse)));
            }
        }
    }
}