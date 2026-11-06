using MISReports_Api.Compat;
using MISReports_Api.DAL.FIFO;
using MISReports_Api.Services.Reporting;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers.FIFO
{
    [RoutePrefix("api/phv-obsolete-idle-fifo")]
    public class PHVObsoleteIdleFIFOController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PHVObsoleteIdleFIFORepository _repository;
        private readonly PHVObsoleteIdleJasperReportService _jasperReportService;

        public PHVObsoleteIdleFIFOController()
        {
            _repository = new PHVObsoleteIdleFIFORepository();
            _jasperReportService = new PHVObsoleteIdleJasperReportService();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetPHVObsoleteIdleFIFO(
            string deptId,
            string warehouseCode,
            int repYear)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                    return BadRequest("deptId and warehouseCode are required.");

                var data = await _repository.GetPHVObsoleteIdleFIFOAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear);

                return Json(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPHVObsoleteIdleFIFO failed. deptId={deptId}, warehouseCode={warehouseCode}, repYear={repYear}", deptId, warehouseCode, repYear);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("pdf")]
        public async Task<HttpResponseMessage> GetPHVObsoleteIdleFIFOPdf(
            string deptId,
            string deptName,
            string warehouseCode,
            int repYear,
            int repMonth,
            bool download = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "deptId and warehouseCode are required.");
                }

                if (repYear <= 0)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "repYear is required.");
                }

                if (repMonth <= 0)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "repMonth is required.");
                }

                var data = await _repository.GetPHVObsoleteIdleFIFOAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear);

                if (data == null || !data.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "No FIFO records were found for the selected period."
                    });
                }

                var costCenterLabel = string.IsNullOrWhiteSpace(deptName)
                    ? deptId.Trim()
                    : $"{deptId.Trim()} - {deptName.Trim()}";

                var pdfBytes = await _jasperReportService.GeneratePhvObsoleteIdleReportAsync(
                    data.Cast<object>(),
                    costCenterLabel,
                    warehouseCode.Trim(),
                    repYear,
                    repMonth,
                    "~/JasperTools/PHVObsoleteIdleReportTool/src/main/resources/reports/phv_obsolete_idle.jrxml",
                    "pdf");

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentLength = pdfBytes.Length;
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(download ? "attachment" : "inline")
                {
                    FileName = $"PHV_Obsolete_Idle_FIFO_{deptId.Trim()}_{warehouseCode.Trim()}_{repYear}_{repMonth:00}.pdf"
                };

                return response;
            }
            catch (JasperReportExecutionException ex)
            {
                logger.Error(ex, "GetPHVObsoleteIdleFIFOPdf failed. deptId={deptId}, deptName={deptName}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}, download={download}", deptId, deptName, warehouseCode, repYear, repMonth, download);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPHVObsoleteIdleFIFOPdf failed. deptId={deptId}, deptName={deptName}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}, download={download}", deptId, deptName, warehouseCode, repYear, repMonth, download);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error generating FIFO PDF.");
            }
        }

        [HttpGet]
        [Route("csv")]
        public async Task<HttpResponseMessage> GetPHVObsoleteIdleFIFOCsv(
            string deptId,
            string deptName,
            string warehouseCode,
            int repYear,
            int repMonth)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "deptId and warehouseCode are required.");
                }

                if (repYear <= 0)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "repYear is required.");
                }

                if (repMonth <= 0)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "repMonth is required.");
                }

                var data = await _repository.GetPHVObsoleteIdleFIFOAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear);

                if (data == null || !data.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, new
                    {
                        success = false,
                        message = "No FIFO records were found for the selected period."
                    });
                }

                var costCenterLabel = string.IsNullOrWhiteSpace(deptName)
                    ? deptId.Trim()
                    : $"{deptId.Trim()} - {deptName.Trim()}";

                var csvBytes = await _jasperReportService.GeneratePhvObsoleteIdleReportAsync(
                    data.Cast<object>(),
                    costCenterLabel,
                    warehouseCode.Trim(),
                    repYear,
                    repMonth,
                    "~/JasperTools/PHVObsoleteIdleReportTool/src/main/resources/reports/phv_obsolete_idle.jrxml",
                    "csv");

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(csvBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                response.Content.Headers.ContentLength = csvBytes.Length;
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"PHV_Obsolete_Idle_FIFO_{deptId.Trim()}_{warehouseCode.Trim()}_{repYear}_{repMonth:00}.csv"
                };

                return response;
            }
            catch (JasperReportExecutionException ex)
            {
                logger.Error(ex, "GetPHVObsoleteIdleFIFOCsv failed. deptId={deptId}, deptName={deptName}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}", deptId, deptName, warehouseCode, repYear, repMonth);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPHVObsoleteIdleFIFOCsv failed. deptId={deptId}, deptName={deptName}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}", deptId, deptName, warehouseCode, repYear, repMonth);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error generating FIFO CSV.");
            }
        }
    }
}