using MISReports_Api.DAL.PIV;
using MISReports_Api.Models.PIV;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/region-wise-vat")]
    public class RegionWiseVatReportController : ApiController
    {
        private readonly RegionWiseVatReportRepository _repo = new RegionWiseVatReportRepository();

        // GET: api/region-wise-vat/report?fromDate=2025/01/01&toDate=2025/12/31&compId=COMP001
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetReport(string fromDate, string toDate, string compId)
        {
            if (string.IsNullOrWhiteSpace(fromDate) ||
                string.IsNullOrWhiteSpace(toDate) ||
                string.IsNullOrWhiteSpace(compId))
            {
                return BadRequest("Parameters 'fromDate', 'toDate' and 'compId' are required (fromDate/toDate format: yyyy/MM/dd)");
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
                List<RegionWiseVatReportModel> data =
                    _repo.GetRegionWiseVatReport(dtFrom, dtTo, compId.Trim());

                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error generating Region Wise VAT report: " + ex.Message));
            }
        }
    }
}