//19. POS Paid PIV Tabulation Summary Report (AFMHQ)
// PosPaidPivTabulationSummaryAfmhqController.cs
using MISReports_Api.Repositories;
using System;
using System.Globalization;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/pos-paid-piv-tabulation-afmhq")]
    public class PosPaidPivTabulationSummaryAfmhqController : ApiController
    {
        private readonly PosPaidPivTabulationSummaryAfmhqRepository _repo = new PosPaidPivTabulationSummaryAfmhqRepository();

        /// <summary>
        /// GET: api/pos-paid-piv-tabulation-afmhq/get?costCtr=AFMHQ&fromDate=20250101&toDate=20251231
        /// POS Paid PIV Tabulation Summary Report (AFMHQ) - Division/Branch/Cost Center wise summary of paid PIVs
        /// </summary>
        [HttpGet]
        [Route("get")]
        public IHttpActionResult Get(string costCtr, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(costCtr) ||
                string.IsNullOrWhiteSpace(fromDate) ||
                string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("costCtr, fromDate and toDate are required.");
            }

            if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fromDt) ||
                !DateTime.TryParseExact(toDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime toDt))
            {
                return BadRequest("Invalid date format. Use yyyyMMdd (e.g. 20250101)");
            }

            try
            {
                var data = _repo.GetPosPaidPivTabulationSummaryAfmhq(costCtr.Trim(), fromDt, toDt);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error fetching POS Paid PIV Tabulation Summary (AFMHQ): " + ex.Message));
            }
        }
    }
}