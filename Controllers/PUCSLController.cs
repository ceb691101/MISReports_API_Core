using MISReports_Api.Compat;
using MISReports_Api.DAL.PUCSLReports.PUCSLSolarConnection;
using MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection;
using MISReports_Api.DAL.PUCSLReports;
using MISReports_Api.Models.PUCSLReports;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    /// <summary>
    /// Controller for PUCSL Solar Connection Summary reports.
    ///
    /// All endpoints accept a JSON POST body (PUCSLRequest):
    /// {
    ///   "reportCategory": "Province" | "Region" | "EntireCEB",
    ///   "typeCode":       "C"  (prov_code or region code; omit for EntireCEB),
    ///   "billCycle":      "440",
    ///   "reportType":     "FixedSolarData" | "VariableSolarData",
    ///   "solarType":      "NetAccounting" | "NetPlus" | "NetPlusPlus"
    /// }
    /// </summary>
    [RoutePrefix("api")]
    public class PUCSLController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly FixedSolarDataDao _fixedSolarDataDao = new FixedSolarDataDao();
        private readonly VariableSolarDataDao _variableSolarDataDao = new VariableSolarDataDao();
        private readonly TotalSolarCustomersDao _totalSolarCustomersDao = new TotalSolarCustomersDao();
        private readonly RawDataForSolarDao _rawDataForSolarDao = new RawDataForSolarDao();
        private readonly NetMeteringDao _netMeteringDao = new NetMeteringDao();
        private readonly BulkPUCSLSolarCustomersDao _bulkPUCSLSolarCustomersDao = new BulkPUCSLSolarCustomersDao();
        private readonly OrdinaryPUCSLSolarCustomersDao _ordinaryPUCSLSolarCustomersDao = new OrdinaryPUCSLSolarCustomersDao();
        private readonly SolarDataUNTCalculationDao _untCalculationDao = new SolarDataUNTCalculationDao();

        [HttpPost]
        [Route("pucsl/solarJournalsRetail")]
        public IActionResult GetSolarJournalsRetail([FromBody] SolarJournalsRetailRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                SolarJournalsRetailDao.ValidateRequest(request);
                var report = new SolarJournalsRetailDao().GetReport(request);
                return Ok(JObject.FromObject(new { data = report, errorMessage = (string)null }));
            }
            catch (ArgumentException ex)
            {
                logger.Warn("GetSolarJournalsRetail rejected: {error}. request={@request}", ex.Message, request);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Cannot retrieve Solar Journals - Retail report.");
                return Content(System.Net.HttpStatusCode.InternalServerError, new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve Solar Journals - Retail report. Please try again."
                });
            }
        }


        // ================================================================
        //  POST  pucsl/solarConnections
        //
        //  Routes to the correct DAO based on request.ReportType.
        // ================================================================
        [HttpPost]
        [Route("pucsl/solarConnections")]
        public IActionResult GetSolarData([FromBody] PUCSLRequest request)
        {
            // ── Null body guard ───────────────────────────────────────
            if (request == null)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Request body is required."
                }));
            }

            // ── Validate common fields ────────────────────────────────
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.BillCycle))
                errors.Add("BillCycle is required.");

            if ((request.ReportCategory == PUCSLReportCategory.Province ||
                 request.ReportCategory == PUCSLReportCategory.Region) &&
                string.IsNullOrWhiteSpace(request.TypeCode))
            {
                errors.Add("TypeCode (province or region code) is required for Province/Region report category.");
            }

            if (errors.Count > 0)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = string.Join(" ", errors)
                }));
            }

            // ── Route by ReportType ───────────────────────────────────
            switch (request.ReportType)
            {
                case PUCSLReportType.FixedSolarData:
                    return ProcessFixedSolarData(request);

                case PUCSLReportType.VariableSolarData:
                    return ProcessVariableSolarData(request);

                case PUCSLReportType.TotalSolarCustomers:
                    return ProcessTotalSolarCustomers(request);

                case PUCSLReportType.RawDataForSolar:
                    return ProcessRawDataForSolar(request);

                case PUCSLReportType.NetMetering:
                    return ProcessNetMetering(request);

                default:
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid ReportType. Valid values: FixedSolarData, VariableSolarData."
                    }));
            }
        }

        // ================================================================
        //  PRIVATE — Fixed Solar Data
        // ================================================================
        private IActionResult ProcessFixedSolarData(PUCSLRequest request)
        {
            try
            {
                if (!_fixedSolarDataDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var data = _fixedSolarDataDao.GetFixedSolarDataReport(request);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (ArgumentException argEx)
            {
                logger.Warn("ProcessFixedSolarData rejected: {error}", argEx.Message);
                return BadRequest(argEx.Message); 
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessFixedSolarData failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve Fixed Solar Data report.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ================================================================
        //  PRIVATE — Variable Solar Data
        // ================================================================
        private IActionResult ProcessVariableSolarData(PUCSLRequest request)
        {
            try
            {
                if (!_variableSolarDataDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var data = _variableSolarDataDao.GetVariableSolarDataReport(request);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (ArgumentException argEx)
            {
                logger.Warn("ProcessVariableSolarData rejected: {error}", argEx.Message);
                return BadRequest(argEx.Message); // Returns 400 with clean message
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessVariableSolarData failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve Variable Solar Data report.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ================================================================
        //  PRIVATE — Total Solar Customers
        // ================================================================
        private IActionResult ProcessTotalSolarCustomers(PUCSLRequest request)
        {
            try
            {
                if (!_totalSolarCustomersDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var data = _totalSolarCustomersDao.GetTotalSolarCustomersReport(request);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessTotalSolarCustomers failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve Total Solar Customers report.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ================================================================
        //  PRIVATE — Raw Data For Solar
        // ================================================================
        private IActionResult ProcessRawDataForSolar(PUCSLRequest request)
        {
            try
            {
                if (!_rawDataForSolarDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var data = _rawDataForSolarDao.GetRawDataForSolarReport(request);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessRawDataForSolar failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Error processing Raw Data for Solar report.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ================================================================
        //  PRIVATE — Net Metering
        // ================================================================
        private IActionResult ProcessNetMetering(PUCSLRequest request)
        {
            try
            {
                if (!_netMeteringDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var data = _netMeteringDao.GetNetMeteringReport(request);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessNetMetering failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Error processing Net Metering report.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ================================================================
        //  POST  pucsl/bulkSolarCustomers
        //
        //  New, standalone endpoint — does not go through GetSolarData /
        //  PUCSLRequest. Body (BulkPUCSLSolarCustomersRequest):
        //  {
        //    "region":        "R1",
        //    "fromBillCycle": "437",
        //    "toBillCycle":   "448"
        //  }
        // ================================================================
        [HttpPost]
        [Route("pucsl/bulkSolarCustomers")]
        public IActionResult GetBulkSolarCustomers([FromBody] BulkPUCSLSolarCustomersRequest request)
        {
            // ── Null body guard ───────────────────────────────────────
            if (request == null)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Request body is required."
                }));
            }

            // ── Validate fields ────────────────────────────────────────
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Region))
                errors.Add("Region is required.");

            if (string.IsNullOrWhiteSpace(request.FromBillCycle))
                errors.Add("FromBillCycle is required.");

            if (string.IsNullOrWhiteSpace(request.ToBillCycle))
                errors.Add("ToBillCycle is required.");

            if (errors.Count > 0)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = string.Join(" ", errors)
                }));
            }

            try
            {
                if (!_bulkPUCSLSolarCustomersDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var data = _bulkPUCSLSolarCustomersDao.GetBulkPUCSLSolarCustomersReport(request);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (ArgumentException argEx)
            {
                logger.Warn("GetBulkSolarCustomers rejected: {error}. request={@request}", argEx.Message, request);
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetBulkSolarCustomers failed. request={@request}", request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve Bulk PUCSL Solar Customers report.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ================================================================
        //  POST  pucsl/ordinarySolarCustomers
        //
        //  New, standalone endpoint — does not go through GetSolarData /
        //  PUCSLRequest. Uses the ordinary Informix connection. Body
        //  (OrdinaryPUCSLSolarCustomersRequest):
        //  {
        //    "region":        "R3",
        //    "fromBillCycle": "437",
        //    "toBillCycle":   "448"
        //  }
        // ================================================================
        [HttpPost]
        [Route("pucsl/ordinarySolarCustomers")]
        public IActionResult GetOrdinarySolarCustomers([FromBody] OrdinaryPUCSLSolarCustomersRequest request)
        {
            // ── Null body guard ───────────────────────────────────────
            if (request == null)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Request body is required."
                }));
            }

            // ── Validate fields ────────────────────────────────────────
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Region))
                errors.Add("Region is required.");

            if (string.IsNullOrWhiteSpace(request.FromBillCycle))
                errors.Add("FromBillCycle is required.");

            if (string.IsNullOrWhiteSpace(request.ToBillCycle))
                errors.Add("ToBillCycle is required.");

            if (errors.Count > 0)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = string.Join(" ", errors)
                }));
            }

            try
            {
                if (!_ordinaryPUCSLSolarCustomersDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var data = _ordinaryPUCSLSolarCustomersDao.GetOrdinaryPUCSLSolarCustomersReport(request);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = (string)null
                }));
            }
            catch (ArgumentException argEx)
            {
                logger.Warn("GetOrdinarySolarCustomers rejected: {error}. request={@request}", argEx.Message, request);
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetOrdinarySolarCustomers failed. request={@request}", request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve Ordinary PUCSL Solar Customers report.",
                    errorDetails = ex.Message
                }));
            }
        }
        
         // ================================================================
        //  POST  pucsl/solarDataUNT
        // ================================================================
        [HttpPost]
        [Route("pucsl/solarDataUNT")]
        public IActionResult GetSolarDataUNT([FromBody] PUCSLRequest request)
        {
            if (request == null)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Request body is required."
                }));
            }

            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.BillCycle))
                errors.Add("BillCycle is required.");

            if ((request.ReportCategory == PUCSLReportCategory.Province ||
                 request.ReportCategory == PUCSLReportCategory.Region) &&
                string.IsNullOrWhiteSpace(request.TypeCode))
            {
                errors.Add("TypeCode (province or region code) is required for Province/Region report category.");
            }

            if (errors.Count > 0)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = string.Join(" ", errors)
                }));
            }

            try
            {
                if (!_untCalculationDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var data = _untCalculationDao.GetSolarDataUNTReport(request);

                return Ok(JObject.FromObject(new
                {
                    data = data,
                    errorMessage = data.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetSolarDataUNT failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Error processing Solar Data UNT Calculation report.",
                    errorDetails = ex.Message
                }));
            }
        }

    }
}
