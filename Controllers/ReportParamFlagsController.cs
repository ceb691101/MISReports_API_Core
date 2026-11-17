using MISReports_Api.Compat;
using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL;
using MISReports_Api.Models.Admin.Report_Parameters;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/report-params")]
    public class ReportParamFlagsController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ReportParamFlagsRepository _repository = new ReportParamFlagsRepository();

        // GET api/report-params/{repId}
        // e.g. GET api/report-params/COSTCENTERTRIAL
        [HttpGet]
        [Route("{repId}")]
        public IActionResult GetReportParamFlags(string repId)
        {
            if (string.IsNullOrWhiteSpace(repId))
                return Content(HttpStatusCode.BadRequest,
                    ApiResponse<object>.Fail("repId is required."));

            try
            {
                var result = _repository.GetReportParamFlags(repId);

                if (result == null)
                    return Content(HttpStatusCode.NotFound,
                        ApiResponse<object>.Fail($"No report found with repId '{repId}'."));

                return Content(HttpStatusCode.OK,
                    ApiResponse<object>.Ok(result, "Report parameter flags fetched successfully."));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetReportParamFlags failed. repId={repId}", repId);
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to fetch report parameter flags: " + ex.Message));
            }
        }
    }
}
