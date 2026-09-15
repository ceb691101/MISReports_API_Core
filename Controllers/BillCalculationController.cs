using System;
using System.Web.Http;
using MISReports_Api.DAL.General.BillCalculation;
using MISReports_Api.Models.General;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/billcalculation")]
    public class BillCalculationController : ApiController
    {
        private readonly BillCalculationDao _dao;

        public BillCalculationController()
        {
            _dao = new BillCalculationDao();
        }

        /// <summary>
        /// Calculate detailed bill with breakdown across multiple tariff periods
        /// This is the main endpoint that handles calculations across different tariff structures
        /// </summary>
        /// <param name="request">Bill calculation request with category, units, fromDate, and toDate</param>
        /// <returns>Detailed bill calculation with period-wise breakdown and block charges</returns>
        /// <example>
        /// POST /api/billcalculation/calculate
        /// Body: {
        ///   "category": 11,
        ///   "fullUnits": 630,
        ///   "fromDate": "2025-02-04",
        ///   "toDate": "2026-01-20"
        /// }
        /// </example>
        [HttpPost]
        [Route("calculate")]
        public IHttpActionResult CalculateBill([FromBody] BillCalculationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                if (request.FullUnits <= 0)
                {
                    return BadRequest("FullUnits must be greater than 0");
                }

                if (request.FromDate >= request.ToDate)
                {
                    return BadRequest("FromDate must be before ToDate");
                }

                var result = _dao.CalculateDetailedBill(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in CalculateBill: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }
    }
}