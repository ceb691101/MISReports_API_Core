using MISReports_Api.Compat;
using MISReports_Api.DAL.PhysicalVerification;
using MISReports_Api.Services.Reporting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/physical-verification-validation")]
    public class PHVValidationController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PHVValidationRepository _repository;
        private readonly PHVValidationJasperReportService _jasperReportService;

        public PHVValidationController()
        {
            _repository = new PHVValidationRepository();
            _jasperReportService = new PHVValidationJasperReportService();
        }

        // GET api/physical-verification-validation?deptId=514.10&repYear=2022&repMonth=11
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetPHVValidationData(
            [FromUri] string deptId,
            [FromUri] string repYear,
            [FromUri] string repMonth)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId))
                    return BadRequest("deptId is required.");

                if (string.IsNullOrWhiteSpace(repYear))
                    return BadRequest("repYear is required.");

                if (string.IsNullOrWhiteSpace(repMonth))
                    return BadRequest("repMonth is required.");

                System.Diagnostics.Trace.WriteLine(
                    $"PHVValidation Request: deptId={deptId}, repYear={repYear}, repMonth={repMonth}"
                );

                var result = await _repository.GetPHVValidationDataAsync(
                    deptId.Trim(),
                    repYear.Trim(),
                    repMonth.Trim()
                );

                if (result == null || !result.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        count = 0,
                        data = result
                    });
                }

                return Ok(new
                {
                    success = true,
                    count = result.Count,
                    data = result
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPHVValidationData failed. deptId={deptId}, repYear={repYear}, repMonth={repMonth}", deptId, repYear, repMonth);
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR in PHVValidationController: {ex}"
                );

                return Ok(new
                {
                    success = false,
                    message = "Error retrieving physical validation data",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        // GET api/physical-verification-validation/pdf?deptId=514.10&deptName=Stores&repYear=2022&repMonth=11&download=false
        [HttpGet]
        [Route("pdf")]
        public async Task<HttpResponseMessage> GetPHVValidationPdf(
            [FromUri] string deptId,
            [FromUri] string deptName,
            [FromUri] string repYear,
            [FromUri] string repMonth,
            [FromUri] bool download = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "deptId is required.");

                if (string.IsNullOrWhiteSpace(repYear))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "repYear is required.");

                if (string.IsNullOrWhiteSpace(repMonth))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "repMonth is required.");

                if (!int.TryParse(repYear.Trim(), out var parsedYear))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "repYear must be a valid year.");

                if (!int.TryParse(repMonth.Trim(), out var parsedMonth))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "repMonth must be a valid month.");

                var data = await _repository.GetPHVValidationDataAsync(
                    deptId.Trim(),
                    repYear.Trim(),
                    repMonth.Trim());

                if (data == null || !data.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "No PHV validation records were found for the selected period."
                    });
                }

                var costCenterLabel = string.IsNullOrWhiteSpace(deptName)
                    ? deptId.Trim()
                    : $"{deptId.Trim()} - {deptName.Trim()}";

                byte[] pdfBytes = await _jasperReportService.GeneratePhvValidationPdfAsync(
                    data,
                    costCenterLabel,
                    parsedYear,
                    parsedMonth);

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentLength = pdfBytes.Length;
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(download ? "attachment" : "inline")
                {
                    FileName = $"PHV_Validation_{deptId.Trim()}_{parsedYear}_{parsedMonth:00}.pdf"
                };

                return response;
            }
            catch (JasperReportExecutionException ex)
            {
                logger.Error(ex, "GetPHVValidationPdf failed. deptId={deptId}, deptName={deptName}, repYear={repYear}, repMonth={repMonth}, download={download}", deptId, deptName, repYear, repMonth, download);
                System.Diagnostics.Trace.WriteLine($"Jasper report execution failed: {ex}");
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPHVValidationPdf failed. deptId={deptId}, deptName={deptName}, repYear={repYear}, repMonth={repMonth}, download={download}", deptId, deptName, repYear, repMonth, download);
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR in PHVValidationController PDF: {ex}"
                );

                return Request.CreateErrorResponse(
                    HttpStatusCode.InternalServerError,
                    "Error generating PHV validation PDF.");
            }
        }
    }
}
