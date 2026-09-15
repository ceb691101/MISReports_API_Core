using MISReports_Api.DAL.PIV;
using MISReports_Api.Models.PIV;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers.PIV
{
    [RoutePrefix("api/area-wise-srp-piv-paid")]
    public class AreaWiseSRPApplicationPIVPaidReportController : ApiController
    {
        private readonly AreaWiseSRPApplicationPIVPaidReportRepository repo =
            new AreaWiseSRPApplicationPIVPaidReportRepository();

        [HttpGet]
        [Route("list")]
        public IHttpActionResult GetReport(string compId, string fromDate, string toDate)
        {
            List<AreaWiseSRPApplicationPIVPaidReportModel> data = repo.GetReport(compId, fromDate, toDate);

            if (data == null || data.Count == 0)
                return Ok(new { message = "No data found." });

            return Ok(data);
        }
    }
}