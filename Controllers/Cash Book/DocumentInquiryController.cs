using MISReports_Api.Models;
using MISReports_Api.Repositories;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/documentinquiry")]
    public class DocumentInquiryController : ApiController
    {
        private readonly DocumentInquiryRepository _repository = new DocumentInquiryRepository();

        // GET api/documentinquiry/cashbook?costCtr=001&fromDate=20220101&toDate=20220131
        [HttpGet]
        [Route("cashbook")]
        public async Task<IHttpActionResult> GetCashBookReport(
            [FromUri] string costCtr,
            [FromUri] string fromDate,
            [FromUri] string toDate)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("Cost center is required");
                if (string.IsNullOrWhiteSpace(fromDate) || fromDate.Length != 8)
                    return BadRequest("fromDate must be in YYYYMMDD format");
                if (string.IsNullOrWhiteSpace(toDate) || toDate.Length != 8)
                    return BadRequest("toDate must be in YYYYMMDD format");

                // Parse YYYYMMDD safely
                if (!DateTime.TryParseExact(fromDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime fDate))
                    return BadRequest("Invalid fromDate format");
                if (!DateTime.TryParseExact(toDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime tDate))
                    return BadRequest("Invalid toDate format");

                System.Diagnostics.Trace.WriteLine($"CashBook Report Request: costCtr={costCtr}, fromDate={fromDate}, toDate={toDate}");

                var data = await _repository.GetCashBookReportAsync(costCtr, fDate, tDate);

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data = data
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in DocumentInquiryController: {ex}");
                return Ok(new
                {
                    success = false,
                    message = "Error retrieving cash book report",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}