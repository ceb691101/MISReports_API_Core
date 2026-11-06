using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL.Admin.Report_Parameters;
using MISReports_Api.Models.Admin.Report_Parameters;
using Newtonsoft.Json.Linq;

namespace MISReports_Api.Controllers.Admin.Report_Parameters
{
    [RoutePrefix("api/reppara")]
    public class RepParaController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly RepParaRepository _repository = new RepParaRepository();

        [HttpGet]
        [Route("")]
        [Route("parameters")]
        [Route("GET_REPORTPARAMS")]
        public IActionResult GetAllReportParams()
        {
            try
            {
                var result = _repository.GetAllReportParams();

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAllReportParams failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get report parameters.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("GET_POPEDREPPARAMS")]
        public IActionResult GetPopedRepParams()
        {
            try
            {
                var result = _repository.GetPopedRepParams();

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPopedRepParams failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get populated report parameters.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("reports")]
        [Route("GET_REPORTS")]
        public IActionResult GetAllReports()
        {
            try
            {
                var result = _repository.GetAllReports();

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAllReports failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get reports.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("parameters")]
        [Route("save-reportparams")]
        [Route("SAVE_REPORTPARAMS")]
        public IActionResult SaveReportParams([FromBody] RepParaModel request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Request body is required."
                    }));
                }

                if (string.IsNullOrWhiteSpace(request.ParaId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "ParaId is required."
                    }));
                }

                var result = _repository.SaveReportParams(request.ParaId, request.ParaDesc);

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "SaveReportParams failed. request={@request}", request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot save report parameters.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("populate")]
        [Route("populateparamts")] // routing ignores letter case, so "POPULATEPARAMTS" still works
        public IActionResult PopulateParamTs([FromBody] PopulateParamTsModel request)
        {
            try
            {
                var result = _repository.PopulateParamTs(request?.RepId, request?.ParamList);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        updatedRows = result.UpdatedRows,
                        success = result.Success,
                        processedReports = result.ProcessedReports,
                        processedParams = result.ProcessedParams,
                        appendedParams = result.AppendedParams,
                        alreadyExistingParams = result.AlreadyExistingParams
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "PopulateParamTs failed. request={@request}", request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot populate parameter list.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// DEBUG endpoint — returns raw populated column values from Oracle so you can
        /// see exactly what the DB stores.  Hit in browser or Postman:
        ///   GET /api/reppara/debug-pending
        /// Remove this endpoint once the issue is resolved.
        /// </summary>
        [HttpGet]
        [Route("debug-pending")]
        public IActionResult DebugPendingParams()
        {
            try
            {
                var rows = _repository.GetRawPopulatedValues();
                return Ok(JObject.FromObject(new
                {
                    data = rows,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DebugPendingParams failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Debug query failed.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("delete-reportparams")]
        [Route("DELETE_REPORTPARAMS")]
        public IActionResult DeleteReportParam([FromBody] RepParaModel request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Request body is required."
                    }));
                }

                if (string.IsNullOrWhiteSpace(request.ParaId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "ParaId is required."
                    }));
                }

                var result = _repository.DeleteReportParam(request.ParaId);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        deletedRows = result.DeletedRows,
                        success = result.DeletedRows > 0
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DeleteReportParam failed. request={@request}", request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot delete report parameter.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}
