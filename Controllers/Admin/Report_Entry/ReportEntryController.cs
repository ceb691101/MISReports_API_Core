using MISReports_Api.Compat;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/reportentry")]
    public class ReportEntryController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly ReportEntryRepository _repository = new ReportEntryRepository();

        private static string NormalizeRepId(string repId)
        {
            return string.IsNullOrWhiteSpace(repId)
                ? string.Empty
                : repId.Trim().ToUpperInvariant();
        }

        [HttpGet]
        [Route("nextid")]
        public IActionResult GetNextReportIdNo()
        {
            try
            {
                var id = _repository.GetNextReportIdNo();
                return Ok(JObject.FromObject(new { data = id, errorMessage = (string)null }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetNextReportIdNo failed");
                return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "Cannot get next ID.", errorDetails = ex.Message }));
            }
        }

        [HttpGet]
        [Route("")]
        public IActionResult GetAllReportEntries([FromUri] string catcode = null)
        {
            try
            {
                var result = string.IsNullOrWhiteSpace(catcode)
                    ? _repository.GetAllReportEntries()
                    : _repository.GetReportEntriesByCategory(catcode);
                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAllReportEntries failed. catcode={catcode}", catcode);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get report entries.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("filter")]
        public IActionResult FilterReportEntries([FromUri] string repid, [FromUri] string catcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(repid) || string.IsNullOrWhiteSpace(catcode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "repid and catcode are required."
                    }));
                }

                var result = _repository.FilterReportEntries(NormalizeRepId(repid), catcode);
                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "FilterReportEntries failed. repid={repid}, catcode={catcode}", repid, catcode);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot filter report entries.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("")]
        public IActionResult AddReportEntry([FromBody] ReportEntryModel request)
        {
            try
            {
                if (request == null)
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "Request body is required." }));

                request.RepId = NormalizeRepId(request.RepId);

                if (request.RepIdNo < 0)
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "RepIdNo cannot be negative. Use 0 or a positive number." }));

                if (string.IsNullOrWhiteSpace(request.RepId) || string.IsNullOrWhiteSpace(request.CatCode))
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "RepId and CatCode are required." }));

                if (_repository.ExistsByRepIdNoAndRepId(request.RepIdNo, request.RepId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = new { success = false },
                        errorMessage = "Same Report ID NO and Report ID already exists."
                    }));
                }

                if (_repository.ExistsByRepIdAndCategory(request.RepId, request.CatCode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = new { success = false },
                        errorMessage = "Report already exists for this category."
                    }));
                }

                var success = _repository.AddReportEntry(request);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = success, message = success ? "Report entry added successfully." : "Failed to add report entry." },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "AddReportEntry failed. request={@request}", request);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = false },
                    errorMessage = "Cannot add report entry.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPut]
        [Route("{repIdNo:int}/{catCode}")]
        public IActionResult EditReportEntry(int repIdNo, string catCode, [FromBody] ReportEntryModel request)
        {
            try
            {
                if (request == null)
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "Request body is required." }));

                if (repIdNo < 0 || request.RepIdNo < 0)
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "RepIdNo cannot be negative. Use 0 or a positive number." }));

                if (string.IsNullOrWhiteSpace(catCode))
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "CatCode is required." }));

                var success = _repository.EditReportEntry(repIdNo, catCode, request);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = success, message = success ? "Report entry updated successfully." : "Report entry not found." },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "EditReportEntry failed. repIdNo={repIdNo}, catCode={catCode}, request={@request}", repIdNo, catCode, request);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = false },
                    errorMessage = "Cannot edit report entry.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpDelete]
        [Route("{repIdNo:int}/{catCode}")]
        public IActionResult DeleteReportEntry(int repIdNo, string catCode)
        {
            try
            {
                if (repIdNo < 0)
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "RepIdNo cannot be negative. Use 0 or a positive number." }));

                if (string.IsNullOrWhiteSpace(catCode))
                    return Ok(JObject.FromObject(new { data = (object)null, errorMessage = "CatCode is required." }));

                var deleteStatus = _repository.GetDeleteStatus(repIdNo, catCode);
                if (deleteStatus == "not_found")
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = new { success = false, message = "Report entry not found." },
                        errorMessage = (string)null
                    }));
                }

                if (deleteStatus == "restricted")
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = new { success = false, message = "Delete not allowed: this report is assigned to roles." },
                        errorMessage = (string)null
                    }));
                }

                if (deleteStatus == "ambiguous")
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = new { success = false, message = "Delete not allowed: multiple entries share this Report ID NO. Please refresh and select the exact row again." },
                        errorMessage = (string)null
                    }));
                }

                var success = _repository.DeleteReportEntry(repIdNo, catCode);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = success, message = success ? "Report entry deleted successfully." : "Failed to delete report entry." },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DeleteReportEntry failed. repIdNo={repIdNo}, catCode={catCode}", repIdNo, catCode);
                return Ok(JObject.FromObject(new
                {
                    data = new { success = false },
                    errorMessage = "Cannot delete report entry.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("upload")]
        public async Task<IActionResult> UploadSampleImage()
        {
            try
            {
                if (!(Request.ContentType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) ?? false))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Unsupported media type. Expected multipart/form-data."
                    }));
                }

                var form = await Request.ReadFormAsync();
                var fileContent = form.Files.FirstOrDefault(f => !string.IsNullOrEmpty(f.FileName));
                if (fileContent == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "No file uploaded."
                    }));
                }

                string rawFileName = fileContent.FileName.Trim('\"');
                string extension = Path.GetExtension(rawFileName)?.ToLowerInvariant();

                if (string.IsNullOrEmpty(extension) || (extension != ".png" && extension != ".jpg" && extension != ".jpeg"))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Only PNG, JPG, and JPEG image files are allowed."
                    }));
                }

                string repId = form["repId"].FirstOrDefault() ?? Request.Query["repId"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(repId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Report ID (repId) is required."
                    }));
                }

                repId = NormalizeRepId(repId);
                string newFileName = $"{repId}{extension}";

                string sampleReportsDir = ResolveSampleReportsDirectory();
                if (!Directory.Exists(sampleReportsDir))
                {
                    Directory.CreateDirectory(sampleReportsDir);
                }

                foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    string existingFile = Path.Combine(sampleReportsDir, $"{repId}{ext}");
                    if (System.IO.File.Exists(existingFile))
                    {
                        try { System.IO.File.Delete(existingFile); } catch { }
                    }
                }

                string destinationPath = Path.Combine(sampleReportsDir, newFileName);
                byte[] fileBytes;
                using (var buffer = new MemoryStream())
                {
                    await fileContent.CopyToAsync(buffer);
                    fileBytes = buffer.ToArray();
                }
                System.IO.File.WriteAllBytes(destinationPath, fileBytes);

                string generatedPath = $"src/assets/SampleReports/{newFileName}";

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = true,
                        path = generatedPath,
                        message = "Image uploaded successfully."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "UploadSampleImage failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot upload sample image.",
                    errorDetails = ex.Message
                }));
            }
        }

        private static string ResolveSampleReportsDirectory()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string frontendSampleDir = Path.GetFullPath(Path.Combine(baseDir, "..", "CEBReport-Frontend-main", "src", "assets", "SampleReports"));
                string frontendAssetsDir = Path.GetFullPath(Path.Combine(baseDir, "..", "CEBReport-Frontend-main", "src", "assets"));

                if (Directory.Exists(frontendAssetsDir) || Directory.Exists(frontendSampleDir))
                {
                    if (!Directory.Exists(frontendSampleDir))
                    {
                        Directory.CreateDirectory(frontendSampleDir);
                    }
                    return frontendSampleDir;
                }

                string appSampleDir = Path.GetFullPath(Path.Combine(baseDir, "src", "assets", "SampleReports"));
                if (!Directory.Exists(appSampleDir))
                {
                    Directory.CreateDirectory(appSampleDir);
                }
                return appSampleDir;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ResolveSampleReportsDirectory failed");
                System.Diagnostics.Debug.WriteLine($"Error resolving sample reports directory: {ex.Message}");
                string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleReports");
                if (!Directory.Exists(fallback))
                {
                    Directory.CreateDirectory(fallback);
                }
                return fallback;
            }
        }
    }
}
