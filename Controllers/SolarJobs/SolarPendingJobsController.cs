using MISReports_Api.DAL.SolarJobs;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers.SolarJobs
{
    [RoutePrefix("api/solarjobs/pending-jobs")]
    public class SolarPendingJobsController : ApiController
    {
        private readonly SolarPendingJobsRepository _repository = new SolarPendingJobsRepository();

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetList([FromUri] string fromDate, [FromUri] string toDate, [FromUri] string provinceId)
        {
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate) || string.IsNullOrWhiteSpace(provinceId))
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "fromDate, toDate and provinceId are required."
                });
            }

            if (!TryParseDate(fromDate, out DateTime parsedFromDate) || !TryParseDate(toDate, out DateTime parsedToDate))
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Invalid date format. Use yyyy/MM/dd, yyyy-MM-dd, or yyyyMMdd."
                });
            }

            if (parsedFromDate.Date > parsedToDate.Date)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "fromDate cannot be greater than toDate."
                });
            }

            try
            {
                var data = await _repository.GetPendingJobsAsync(parsedFromDate, parsedToDate, provinceId);

                return Ok(new
                {
                    data,
                    errorMessage = (string)null,
                    count = data.Count
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Solar Pending Jobs report data.",
                    errorDetails = ex.Message
                });
            }
        }

        private static bool TryParseDate(string value, out DateTime result)
        {
            return DateTime.TryParseExact(value, new[] { "yyyy/MM/dd", "yyyy-MM-dd", "yyyyMMdd" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
    }
}
