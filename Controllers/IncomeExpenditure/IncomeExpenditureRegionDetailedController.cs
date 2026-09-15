//Consolidated Income & Expenditure Regional Statement(Report)

// File: IncomeExpenditureRegionDetailedController.cs
using MISReports_Api.Repositories;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    public class IncomeExpenditureRegionDetailedController : ApiController
    {
        private readonly IncomeExpenditureRegionDetailedRepository _repository;

        public IncomeExpenditureRegionDetailedController()
        {
            _repository = new IncomeExpenditureRegionDetailedRepository();
        }

        // GET: api/IncomeExpenditureRegionDetailed?compId=XXX&year=2025&month=12
        [HttpGet]
        public IHttpActionResult Get(string compId, string year, string month)
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
                // Log exception here if you have logging in place
                return InternalServerError(ex);
            }
        }
    }
}