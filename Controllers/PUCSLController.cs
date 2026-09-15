using MISReports_Api.DAL.PUCSLReports.PUCSLSolarConnection;
using MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection;
using MISReports_Api.DAL.PUCSLReports;
using MISReports_Api.Models.PUCSLReports;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

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
        private readonly FixedSolarDataDao _fixedSolarDataDao = new FixedSolarDataDao();
        private readonly VariableSolarDataDao _variableSolarDataDao = new VariableSolarDataDao();
        private readonly TotalSolarCustomersDao _totalSolarCustomersDao = new TotalSolarCustomersDao();
        private readonly RawDataForSolarDao _rawDataForSolarDao = new RawDataForSolarDao();
        private readonly NetMeteringDao _netMeteringDao = new NetMeteringDao();
        private readonly BulkPUCSLSolarCustomersDao _bulkPUCSLSolarCustomersDao = new BulkPUCSLSolarCustomersDao();
        private readonly OrdinaryPUCSLSolarCustomersDao _ordinaryPUCSLSolarCustomersDao = new OrdinaryPUCSLSolarCustomersDao();
        private readonly SolarDataUNTCalculationDao _untCalculationDao = new SolarDataUNTCalculationDao();


        // ================================================================
        //  POST  pucsl/solarConnections
        //
        //  Routes to the correct DAO based on request.ReportType.
        // ================================================================
        [HttpPost]
        [Route("pucsl/solarConnections")]
        public IHttpActionResult GetSolarData([FromBody] PUCSLRequest request)
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
        private IHttpActionResult ProcessFixedSolarData(PUCSLRequest request)
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
                return BadRequest(argEx.Message); 
            }
            catch (Exception ex)
            {
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
        private IHttpActionResult ProcessVariableSolarData(PUCSLRequest request)
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
                return BadRequest(argEx.Message); // Returns 400 with clean message
            }
            catch (Exception ex)
            {
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
        private IHttpActionResult ProcessTotalSolarCustomers(PUCSLRequest request)
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
        private IHttpActionResult ProcessRawDataForSolar(PUCSLRequest request)
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
        private IHttpActionResult ProcessNetMetering(PUCSLRequest request)
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
        public IHttpActionResult GetBulkSolarCustomers([FromBody] BulkPUCSLSolarCustomersRequest request)
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
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
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
        public IHttpActionResult GetOrdinarySolarCustomers([FromBody] OrdinaryPUCSLSolarCustomersRequest request)
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
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
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
        public IHttpActionResult GetSolarDataUNT([FromBody] PUCSLRequest request)
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