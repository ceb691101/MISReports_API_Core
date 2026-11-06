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
    [RoutePrefix("api/phv-damage-fifo")]
    public class PHVDamageFIFOController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PHVDamageFIFORepository _repository;
        private readonly PHVObsoleteIdleJasperReportService _jasperReportService;

        public PHVDamageFIFOController()
        {
            _repository = new PHVDamageFIFORepository();
            _jasperReportService = new PHVObsoleteIdleJasperReportService();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> GetPHVDamageFIFO(
            string deptId,
            string warehouseCode,
            int repYear)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                    return BadRequest("deptId and warehouseCode are required.");

                var data = await _repository.GetPHVDamageFIFOAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear);

                return Json(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPHVDamageFIFO failed. deptId={deptId}, warehouseCode={warehouseCode}, repYear={repYear}", deptId, warehouseCode, repYear);
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("pdf")]
        public async Task<HttpResponseMessage> GetPHVDamageFIFOPdf(
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

                var data = await _repository.GetPHVDamageFIFOAsync(
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
                    "~/JasperTools/PHVObsoleteIdleReportTool/src/main/resources/reports/PhysicalVerification_Damage_FIFO_new.jrxml",
                    "pdf");

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(pdfBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                response.Content.Headers.ContentLength = pdfBytes.Length;
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(download ? "attachment" : "inline")
                {
                    FileName = $"PHV_Damage_FIFO_{deptId.Trim()}_{warehouseCode.Trim()}_{repYear}_{repMonth:00}.pdf"
                };

                return response;
            }
            catch (JasperReportExecutionException ex)
            {
                logger.Error(ex, "GetPHVDamageFIFOPdf failed. deptId={deptId}, deptName={deptName}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}, download={download}", deptId, deptName, warehouseCode, repYear, repMonth, download);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPHVDamageFIFOPdf failed. deptId={deptId}, deptName={deptName}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}, download={download}", deptId, deptName, warehouseCode, repYear, repMonth, download);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error generating FIFO PDF.");
            }
        }

        [HttpGet]
        [Route("csv")]
        public async Task<HttpResponseMessage> GetPHVDamageFIFOCsv(
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

                var data = await _repository.GetPHVDamageFIFOAsync(
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
                    "~/JasperTools/PHVObsoleteIdleReportTool/src/main/resources/reports/PhysicalVerification_Damage_FIFO_new.jrxml",
                    "csv");

                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(csvBytes);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
                response.Content.Headers.ContentLength = csvBytes.Length;
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = $"PHV_Damage_FIFO_{deptId.Trim()}_{warehouseCode.Trim()}_{repYear}_{repMonth:00}.csv"
                };

                return response;
            }
            catch (JasperReportExecutionException ex)
            {
                logger.Error(ex, "GetPHVDamageFIFOCsv failed. deptId={deptId}, deptName={deptName}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}", deptId, deptName, warehouseCode, repYear, repMonth);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPHVDamageFIFOCsv failed. deptId={deptId}, deptName={deptName}, warehouseCode={warehouseCode}, repYear={repYear}, repMonth={repMonth}", deptId, deptName, warehouseCode, repYear, repMonth);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Error generating FIFO CSV.");
            }
        }
    }
}
