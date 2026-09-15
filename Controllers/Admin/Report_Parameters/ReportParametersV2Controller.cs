using System;
using System.Net;
using System.Web.Http;
using MISReports_Api.Models.Admin.Report_Parameters;
using MISReports_Api.Services.Admin.Report_Parameters;

namespace MISReports_Api.Controllers.Admin.Report_Parameters
{
    [RoutePrefix("api")]
    public class ReportParametersV2Controller : ApiController
    {
        private readonly IReportParameterService _service;

        public ReportParametersV2Controller() : this(new ReportParameterService())
        {
        }

        public ReportParametersV2Controller(IReportParameterService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("parameters")]
        public IHttpActionResult GetParameters()
        {
            try
            {
                var data = _service.GetParameters();
                return Content(HttpStatusCode.OK, ApiResponse<object>.Ok(data, "Parameters fetched successfully."));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to fetch parameters: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("parameters")]
        public IHttpActionResult SaveParameter([FromBody] ParameterRequestModel request)
        {
            if (request == null)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail("Request body is required."));
            }

            try
            {
                var result = _service.SaveParameter(request.Name, request.Description);
                return Content(HttpStatusCode.OK, ApiResponse<object>.Ok(result, "Parameter saved successfully."));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to save parameter: " + ex.Message));
            }
        }

        [HttpDelete]
        [Route("parameters/{name}")]
        public IHttpActionResult DeleteParameter(string name)
        {
            try
            {
                var deletedRows = _service.DeleteParameter(name);
                return Content(HttpStatusCode.OK,
                    ApiResponse<object>.Ok(new { deletedRows = deletedRows }, "Parameter deleted successfully."));
            }
            catch (ArgumentException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to delete parameter: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("reports")]
        public IHttpActionResult GetReports()
        {
            try
            {
                var data = _service.GetReports();
                return Content(HttpStatusCode.OK, ApiResponse<object>.Ok(data, "Reports fetched successfully."));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to fetch reports: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("populate")]
        public IHttpActionResult Populate()
        {
            try
            {
                var result = _service.Populate();
                return Content(HttpStatusCode.OK,
                    ApiResponse<object>.Ok(result, "Populate completed successfully."));
            }
            catch (InvalidOperationException ex)
            {
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Populate failed: " + ex.Message));
            }
        }
    }
}
