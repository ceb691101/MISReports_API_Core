using MISReports_Api.Compat;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/provinceincomeexpenditure")]
    public class ProvinceIncomeExpenditureController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ProvinceIncomeExpenditureRepository _repo = new ProvinceIncomeExpenditureRepository();

        [HttpGet]
        [Route("{compId}/{repYear}/{repMonth}")]
        public IActionResult GetProvinceIncomeExpenditure(string compId, string repYear, string repMonth)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(compId))
                    return Content(HttpStatusCode.BadRequest, new { success = false, errorType = "ValidationError", errorMessage = "Company ID is required" });

                if (!int.TryParse(repYear, out int y) || y < 1900 || y > 2100)
                    return Content(HttpStatusCode.BadRequest, new { success = false, errorType = "ValidationError", errorMessage = "Invalid year" });

                if (!int.TryParse(repMonth, out int m) || m < 1 || m > 12)
                    return Content(HttpStatusCode.BadRequest, new { success = false, errorType = "ValidationError", errorMessage = "Invalid month (1-12)" });

                var data = _repo.GetProvinceIncomeExpenditure(compId, repYear, repMonth);

                return Ok(new
                {
                    success = true,
                    data,
                    recordCount = data.Count,
                    parameters = new { compId, repYear, repMonth },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetProvinceIncomeExpenditure failed. compId={compId}, repYear={repYear}, repMonth={repMonth}", compId, repYear, repMonth);
                Debug.WriteLine(ex);
                return Content(HttpStatusCode.InternalServerError, new
                {
                    success = false,
                    data = new List<ProvinceIncomeExpenditureModel>(),
                    recordCount = 0,
                    errorType = "SystemError",
                    errorMessage = "Cannot get Province Income over Expenditure data.",
                    errorDetails = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }
    }
}