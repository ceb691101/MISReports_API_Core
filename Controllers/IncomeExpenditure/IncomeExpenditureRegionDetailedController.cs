//Consolidated Income & Expenditure Regional Statement(Report)

// File: IncomeExpenditureRegionDetailedController.cs
using MISReports_Api.Compat;
using MISReports_Api.Repositories;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    // Was reached through the Web API convention route "api/{controller}"; same URL kept.
    [Route("api/IncomeExpenditureRegionDetailed")]
    public class IncomeExpenditureRegionDetailedController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IncomeExpenditureRegionDetailedRepository _repository;

        public IncomeExpenditureRegionDetailedController()
        {
            _repository = new IncomeExpenditureRegionDetailedRepository();
        }

        // GET: api/IncomeExpenditureRegionDetailed?compId=XXX&year=2025&month=12
        [HttpGet]
        public IActionResult Get(string compId, string year, string month)
        {
            if (string.IsNullOrWhiteSpace(compId) ||
                string.IsNullOrWhiteSpace(year) ||
                string.IsNullOrWhiteSpace(month))
            {
                return BadRequest("compId, year, and month parameters are required.");
            }

            try
            {
                var data = _repository.GetIncomeExpenditureRegionDetailedReport(compId, year, month);
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Get failed. compId={compId}, year={year}, month={month}", compId, year, month);
                // Log exception here if you have logging in place
                return InternalServerError(ex);
            }
        }
    }
}