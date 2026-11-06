using MISReports_Api.Compat;
using MISReports_Api.DAL;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/workinprogresscompletedcostcenterwise")]
    public class WorkInProgressCompletedCostCenterwiseController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly WorkInProgressCompletedCostCenterwiseRepository _repo;

        public WorkInProgressCompletedCostCenterwiseController()
        {
            _repo = new WorkInProgressCompletedCostCenterwiseRepository();
        }

        /// <summary>
        /// Get Work In Progress Completed Cost Center wise data
        /// Example:
        /// GET api/workinprogresscompletedcostcenterwise/510.30/2007-01-01/2008-12-31
        /// </summary>
        [HttpGet]
        [Route("{costctr}/{fromDate}/{toDate}")]
        public async Task<IActionResult> GetWorkInProgressCompletedCostCenterwise(string costctr, string fromDate, string toDate)
        {
            try
            {
                // Pass strings directly to repository
                var result = await _repo.GetWorkInProgressCompletedCostCenterwise(costctr, fromDate, toDate);

                return Ok(new
                {
                    data = result,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetWorkInProgressCompletedCostCenterwise failed. costctr={costctr}, fromDate={fromDate}, toDate={toDate}", costctr, fromDate, toDate);
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = ex.Message
                });
            }
        }
    }
}
