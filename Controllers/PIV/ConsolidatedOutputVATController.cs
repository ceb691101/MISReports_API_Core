using MISReports_Api.DAL.PIV;
using MISReports_Api.Models.PIV;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/consolidatedvat")]
    public class ConsolidatedOutputVATController : ApiController
    {
        private readonly ConsolidatedOutputVATRepository _repo = new ConsolidatedOutputVATRepository();

        // GET: api/consolidatedvat/report?fromDate=2025/01/01&toDate=2025/12/31
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetReport(string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("Both 'fromDate' and 'toDate' parameters are required (format: yyyy/MM/dd)");
            }

            if (!DateTime.TryParseExact(fromDate, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.None, out DateTime dtFrom) ||
                !DateTime.TryParseExact(toDate, "yyyy/MM/dd", null, System.Globalization.DateTimeStyles.None, out DateTime dtTo))
            {
                return BadRequest("Invalid date format. Use yyyy/MM/dd");
            }

            if (dtFrom > dtTo)
            {
                return BadRequest("fromDate cannot be later than toDate");
            }

            try
            {
                List<ConsolidatedOutputVATModel> data = _repo.GetConsolidatedOutputVAT(dtFrom, dtTo);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error while generating Consolidated Output VAT report: " + ex.Message));
            }
        }
    }
}