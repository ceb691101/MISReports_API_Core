using MISReports_Api.Compat;
using System;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/smc-line-details-by-province")]
    public class SMCLineDetailsByProvinceController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly SMCLineDetailsByProvinceDAL _dal =
            new SMCLineDetailsByProvinceDAL();



        [HttpGet]
        [Route("companies")]
        public IActionResult GetActiveCompanies()
        {
            try
            {
                var data = _dal.GetActiveCompanies();

                return Ok(new
                {
                    data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetActiveCompanies failed");
                System.Diagnostics.Debug.WriteLine(
                    $"Error loading active companies: {ex.Message}\n{ex.StackTrace}");

                return InternalServerError(ex);
            }
        }
        // QUERY URL:
        // /api/smc-line-details-by-province
        // ?compId=NCP
        // &fromDate=2026-01-01  
        // &toDate=2026-10-07
        // &wiringType=OH
        [HttpGet]
        [Route("")]
        public IActionResult GetQuery(
            [FromUri] string compId,
            [FromUri] string fromDate,
            [FromUri] string toDate,
            [FromUri] string wiringType)
        {
            return ExecuteQuery(
                compId,
                fromDate,
                toDate,
                wiringType);
        }

        // PATH URL:
        // /api/smc-line-details-by-province/
        // NCP/2026-01-01/2026-10-07/OH
        [HttpGet]
        [Route("{compId}/{fromDate}/{toDate}/{wiringType}")]
        public IActionResult GetPath(
            string compId,
            string fromDate,
            string toDate,
            string wiringType)
        {
            return ExecuteQuery(
                compId,
                fromDate,
                toDate,
                wiringType);
        }

        private IActionResult ExecuteQuery(
            string compId,
            string fromDate,
            string toDate,
            string wiringType)
        {
            try
            {
                // Validate Company / Province
                if (string.IsNullOrWhiteSpace(compId))
                {
                    return BadRequest(
                        "compId is required.");
                }

                // Validate From Date
                if (string.IsNullOrWhiteSpace(fromDate) ||
                    !DateTime.TryParse(
                        fromDate,
                        out DateTime fromDt))
                {
                    return BadRequest(
                        "fromDate must be a valid date (e.g., 2026-01-01).");
                }

                // Validate To Date
                if (string.IsNullOrWhiteSpace(toDate) ||
                    !DateTime.TryParse(
                        toDate,
                        out DateTime toDt))
                {
                    return BadRequest(
                        "toDate must be a valid date (e.g., 2026-10-07).");
                }

                // Validate date range
                if (toDt < fromDt)
                {
                    return BadRequest(
                        "toDate cannot be earlier than fromDate.");
                }

                string fromDateFormatted =
                    fromDt.ToString("yyyy/MM/dd");

                string toDateFormatted =
                    toDt.ToString("yyyy/MM/dd");

                /*
                 * ALL means:
                 * Do not filter by wiring type.
                 *
                 * OH / UG / any other database value:
                 * Pass the selected value to the DAL.
                 */
                string wiringTypeFilter =
                    wiringType;

                if (string.Equals(
                        wiringType,
                        "ALL",
                        StringComparison.OrdinalIgnoreCase))
                {
                    wiringTypeFilter = null;
                }

                var data =
                    _dal.GetSMCLineDetailsByProvince(
                        compId.Trim(),
                        fromDateFormatted,
                        toDateFormatted,
                        wiringTypeFilter);

                return Ok(new
                {
                    data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ExecuteQuery failed. compId={compId}, fromDate={fromDate}, toDate={toDate}, wiringType={wiringType}", compId, fromDate, toDate, wiringType);
                System.Diagnostics.Debug.WriteLine(
                    $"Error in SMCLineDetailsByProvinceController: " +
                    $"{ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage =
                        "Cannot get SMC line details.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}
