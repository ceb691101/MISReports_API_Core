//26. C/C PIV Details (Status Report)

using MISReports_Api.DAL.PIV;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/costcenter-piv")]
    public class CostCenterwisePivdetailsController : ApiController
    {
        private readonly CostCenterwisePivdetailsRepository _repo = new CostCenterwisePivdetailsRepository();

        // GET: api/costcenter-piv/report?costctr=440.00&fromDate=2026/01/01&toDate=2026/01/02
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetReport(string costctr, string fromDate, string toDate)
        {
            if (string.IsNullOrWhiteSpace(costctr))
            {
                return BadRequest("Parameter 'costctr' (cost center code) is required");
            }

            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
            {
                return BadRequest("Parameters 'fromDate' and 'toDate' are required (format: yyyy/MM/dd)");
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
                var data = _repo.GetCostCenterPivDetails(costctr, dtFrom, dtTo);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("Error generating Cost Center PIV Report: " + ex.Message));
            }
        }
    }
}