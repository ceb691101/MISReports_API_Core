using MISReports_Api.Compat;
using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.Models.Admin.Report_Parameters;
using MISReports_Api.Services.Admin.Report_Parameters;

namespace MISReports_Api.Controllers.Admin.Report_Parameters
{
    [RoutePrefix("api")]
    public class ReportParametersV2Controller : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IReportParameterService _service;

        // Web API always used this constructor; tell ASP.NET Core to do the same.

        [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]

        public ReportParametersV2Controller() : this(new ReportParameterService())
        {
        }

        public ReportParametersV2Controller(IReportParameterService service)
        {
            _service = service;
        }

        [HttpGet]
        [Route("parameters")]
        public IActionResult GetParameters()
        {
            try
            {
                var data = _service.GetParameters();
                return Content(HttpStatusCode.OK, ApiResponse<object>.Ok(data, "Parameters fetched successfully."));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetParameters failed");
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to fetch parameters: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("parameters")]
        public IActionResult SaveParameter([FromBody] ParameterRequestModel request)
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
                logger.Warn("SaveParameter rejected: {error}. request={@request}", ex.Message, request);
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "SaveParameter failed. request={@request}", request);
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to save parameter: " + ex.Message));
            }
        }

        [HttpDelete]
        [Route("parameters/{name}")]
        public IActionResult DeleteParameter(string name)
        {
            try
            {
                var deletedRows = _service.DeleteParameter(name);
                return Content(HttpStatusCode.OK,
                    ApiResponse<object>.Ok(new { deletedRows = deletedRows }, "Parameter deleted successfully."));
            }
            catch (ArgumentException ex)
            {
                logger.Warn("DeleteParameter rejected: {error}. name={name}", ex.Message, name);
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DeleteParameter failed. name={name}", name);
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to delete parameter: " + ex.Message));
            }
        }

        [HttpGet]
        [Route("reports")]
        public IActionResult GetReports()
        {
            try
            {
                var data = _service.GetReports();
                return Content(HttpStatusCode.OK, ApiResponse<object>.Ok(data, "Reports fetched successfully."));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetReports failed");
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Failed to fetch reports: " + ex.Message));
            }
        }

        [HttpPost]
        [Route("populate")]
        public IActionResult Populate()
        {
            try
            {
                var result = _service.Populate();
                return Content(HttpStatusCode.OK,
                    ApiResponse<object>.Ok(result, "Populate completed successfully."));
            }
            catch (InvalidOperationException ex)
            {
                logger.Warn("Populate rejected: {error}", ex.Message);
                return Content(HttpStatusCode.BadRequest, ApiResponse<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Populate failed");
                return Content(HttpStatusCode.InternalServerError,
                    ApiResponse<object>.Fail("Populate failed: " + ex.Message));
            }
        }
    }
}
