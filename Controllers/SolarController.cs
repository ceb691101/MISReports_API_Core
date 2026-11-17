using MISReports_Api.Compat;
using MISReports_Api.DAL.SolarInformation.SolarProgressClarification;
using MISReports_Api.DAL.SolarInformation.SolarPVConnections;
using MISReports_Api.DAL.SolarInformation.SolarPaymentRetail;
using MISReports_Api.DAL.SolarInformation.SolarPVCapacity;
using MISReports_Api.DAL.SolarInformation.SolarConnectionDetails;
using MISReports_Api.DAO.SolarInformation.SolarCustomerInformation;
using MISReports_Api.DAL.SolarInformation.RoofTopSolarInputData;
using MISReports_Api.DAL.SolarInformation;
using MISReports_Api.Models.SolarInformation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;


namespace MISReports_Api.Controllers
{
    [RoutePrefix("solarapi")]
    public class SolarController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        
        private readonly DetailedDao _detailedDao = new DetailedDao();
        private readonly SummaryDao _summaryDao = new SummaryDao();
        private readonly PVConnectionDao _pvConnectionDao = new PVConnectionDao();
        private readonly PVBulkConnectionDao _pvBulkConnectionDao = new PVBulkConnectionDao();
        private readonly OrdinaryDetailedDao _ordinaryDetailedDao = new OrdinaryDetailedDao();
        private readonly OrdinarySummaryDao _ordinarySummaryDao = new OrdinarySummaryDao();
        private readonly RetailDetailedDao _retailDetailedDao = new RetailDetailedDao();
        private readonly OrdSummaryDao _ordSummaryDao = new OrdSummaryDao();
        private readonly BulkSummaryDao _bulkSummaryDao = new BulkSummaryDao();
        private readonly PVCapacityOrdinaryDao _pvCapacityOrdinaryDao = new PVCapacityOrdinaryDao();
        private readonly PVCapacityBulkDao _pvCapacityBulkDao = new PVCapacityBulkDao();
        private readonly PVCapacitySummaryDao _pvCapacitySummaryDao = new PVCapacitySummaryDao();
        private readonly SolarPaymentBulkDao _solarPaymentBulkDao = new SolarPaymentBulkDao();
        private readonly SolarRooftopPaymentsandExportUnitsDao _solarRooftopPaymentsandExportUnitsDao = new SolarRooftopPaymentsandExportUnitsDao();
        private readonly SolarReadingRetailDetailedDao _solarReadingDetailedDao = new SolarReadingRetailDetailedDao();
        private readonly SolarReadingRetailSummaryDao _solarReadingSummaryDao = new SolarReadingRetailSummaryDao();
        private readonly SolarReadingUsageBulkDao _solarReadingUsageBulkDao = new SolarReadingUsageBulkDao();
        private readonly SolarCustomerInforOrdinaryDao _solarCustomerInforOrdinaryDao = new SolarCustomerInforOrdinaryDao();
        private readonly SolarCustomerInforBulkDao _solarCustomerInforBulkDao = new SolarCustomerInforBulkDao();
        private readonly RoofTopSolarInputDataDao _roofTopSolarInputDataDao = new RoofTopSolarInputDataDao();
        private readonly SolarPaymentLissReconDao _solarPaymentLissReconDao = new SolarPaymentLissReconDao();
        private readonly SolarJournalsBulkDao _solarJournalsBulkDao = new SolarJournalsBulkDao();


        [HttpGet]
        [Route("solar-progress/detailed")]
        public IActionResult GetDetailedReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessDetailedRequest(request);
        }

        [HttpGet]
        [Route("solar-progress/summary")]
        public IActionResult GetSummaryReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessSummaryRequest(request);
        }

        [HttpGet]
        [Route("solar-progress/ordinary/detailed")]
        public IActionResult GetOrdinaryDetailedReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessOrdinaryDetailedRequest(request);
        }

        private IActionResult ProcessOrdinaryDetailedRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_ordinaryDetailedDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _ordinaryDetailedDao.GetOrdinaryDetailedReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessOrdinaryDetailedRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar progress data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("solar-progress/ordinary/summary")]
        public IActionResult GetOrdinarySummaryReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessOrdinarySummaryRequest(request);
        }

        private IActionResult ProcessOrdinarySummaryRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_ordinarySummaryDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _ordinarySummaryDao.GetSummaryReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessOrdinarySummaryRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar progress data.",
                    errorDetails = ex.Message
                });
            }
        }

        private IActionResult ProcessDetailedRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_detailedDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _detailedDao.GetDetailedReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessDetailedRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar progress data.",
                    errorDetails = ex.Message
                });
            }
        }

        private IActionResult ProcessSummaryRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_summaryDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _summaryDao.GetSummaryReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessSummaryRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar progress data.",
                    errorDetails = ex.Message
                });
            }
        }

        /// <summary>
        /// Common validation method for report type parameters
        /// </summary>
        private string ValidateReportTypeParameters(
            SolarReportType reportType,
            string areaCode,
            string provCode,
            string region)
        {
            switch (reportType)
            {
                case SolarReportType.Area:
                    if (string.IsNullOrEmpty(areaCode))
                        return "Area code is required for Area report type.";
                    break;
                case SolarReportType.Province:
                    if (string.IsNullOrEmpty(provCode))
                        return "Province code is required for Province report type.";
                    break;
                case SolarReportType.Region:
                    if (string.IsNullOrEmpty(region))
                        return "Region is required for Region report type.";
                    break;
                case SolarReportType.EntireCEB:
                    break;
                default:
                    return "Invalid report type specified.";
            }

            return null;
        }

        private string ValidateRequestParameters(SolarProgressRequest request)
        {
            return ValidateReportTypeParameters(
                request.ReportType,
                request.AreaCode,
                request.ProvCode,
                request.Region
            );
        }

        [HttpGet]
        [Route("pv-connections")]
        public IActionResult GetPVConnections(
            [FromUri] string billCycle = null,
            [FromUri] string calcCycle = null,
            [FromUri] string cycleType = "A", // A for bill_cycle, C for calc_cycle
            [FromUri] string reportType = "entireceb",
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            // Validate cycle parameters
            if (cycleType == "A" && string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required when cycle type is 'A'.");

            if (cycleType == "C" && string.IsNullOrWhiteSpace(calcCycle))
                validationErrors.Add("Calc cycle is required when cycle type is 'C'.");

            if (cycleType != "A" && cycleType != "C")
                validationErrors.Add("Cycle type must be 'A' (bill cycle) or 'C' (calc cycle).");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarPVConnectionRequest
            {
                BillCycle = billCycle,
                CalcCycle = calcCycle,
                CycleType = cycleType
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessPVConnectionRequest(request);
        }

        [HttpGet]
        [Route("pv-bulkconnections")]
        public IActionResult GetPVBulkConnections(
            [FromUri] string billCycle = null,
            [FromUri] string calcCycle = null,
            [FromUri] string cycleType = "A", // A for bill_cycle, C for calc_cycle
            [FromUri] string reportType = "entireceb",
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            // Validate cycle parameters
            if (cycleType == "A" && string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required when cycle type is 'A'.");

            if (cycleType == "C" && string.IsNullOrWhiteSpace(calcCycle))
                validationErrors.Add("Calc cycle is required when cycle type is 'C'.");

            if (cycleType != "A" && cycleType != "C")
                validationErrors.Add("Cycle type must be 'A' (bill cycle) or 'C' (calc cycle).");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarPVBulkConnectionRequest
            {
                BillCycle = billCycle,
                CalcCycle = calcCycle,
                CycleType = cycleType
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessPVBulkConnectionRequest(request);
        }

        private IActionResult ProcessPVBulkConnectionRequest(SolarPVBulkConnectionRequest request)
        {
            try
            {
                if (!_pvConnectionDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidatePVBulkConnectionParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _pvBulkConnectionDao.GetPVConnections(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessPVBulkConnectionRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get PV connections data.",
                    errorDetails = ex.Message
                });
            }
        }

        private string ValidatePVBulkConnectionParameters(SolarPVBulkConnectionRequest request)
        {
            return ValidateReportTypeParameters(
                request.ReportType,
                request.AreaCode,
                request.ProvCode,
                request.Region
            );
        }



        private IActionResult ProcessPVConnectionRequest(SolarPVConnectionRequest request)
        {
            try
            {
                if (!_pvConnectionDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidatePVConnectionParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _pvConnectionDao.GetPVConnections(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessPVConnectionRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get PV connections data.",
                    errorDetails = ex.Message
                });
            }
        }

        private string ValidatePVConnectionParameters(SolarPVConnectionRequest request)
        {
            return ValidateReportTypeParameters(
                request.ReportType,
                request.AreaCode,
                request.ProvCode,
                request.Region
            );
        }

        [HttpGet]
        [Route("retail/detailed")]
        public IActionResult GetRetailDetailedReport(
            [FromUri] string billCycle = null,
            [FromUri] string calcCycle = null,
            [FromUri] string cycleType = "A", // A for bill_cycle, C for calc_cycle
            [FromUri] string netType = "1",   // Net type filter (1, 2, 3, 4, 5)
            [FromUri] string reportType = "entireceb",
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            // Validate cycle parameters
            if (cycleType == "A" && string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required when cycle type is 'A'.");

            if (cycleType == "C" && string.IsNullOrWhiteSpace(calcCycle))
                validationErrors.Add("Calc cycle is required when cycle type is 'C'.");

            if (cycleType != "A" && cycleType != "C")
                validationErrors.Add("Cycle type must be 'A' (bill cycle) or 'C' (calc cycle).");

            // Validate net type
            if (string.IsNullOrWhiteSpace(netType))
                validationErrors.Add("Net type is required.");
            else if (!new[] { "1", "2", "3", "4", "5" }.Contains(netType))
                validationErrors.Add("Net type must be 1, 2, 3, 4, or 5.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new RetailDetailedRequest
            {
                BillCycle = billCycle,
                CalcCycle = calcCycle,
                CycleType = cycleType,
                NetType = netType
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessRetailDetailedRequest(request);
        }

        private IActionResult ProcessRetailDetailedRequest(RetailDetailedRequest request)
        {
            try
            {
                if (!_retailDetailedDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRetailDetailedParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _retailDetailedDao.GetRetailDetailedReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessRetailDetailedRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get retail detailed report data.",
                    errorDetails = ex.Message
                });
            }
        }

        private string ValidateRetailDetailedParameters(RetailDetailedRequest request)
        {
            return ValidateReportTypeParameters(
                request.ReportType,
                request.AreaCode,
                request.ProvCode,
                request.Region
            );
        }

        [HttpGet]
        [Route("retail/summary")]
        public IActionResult GetRetailSummaryReport(
            [FromUri] string billCycle = null,
            [FromUri] string calcCycle = null,
            [FromUri] string cycleType = "A") // A for bill_cycle, C for calc_cycle
        {
            var validationErrors = new List<string>();

            // Validate cycle parameters
            if (cycleType == "A" && string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required when cycle type is 'A'.");

            if (cycleType == "C" && string.IsNullOrWhiteSpace(calcCycle))
                validationErrors.Add("Calc cycle is required when cycle type is 'C'.");

            if (cycleType != "A" && cycleType != "C")
                validationErrors.Add("Cycle type must be 'A' (bill cycle) or 'C' (calc cycle).");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new RetailSummaryRequest
            {
                BillCycle = billCycle,
                CalcCycle = calcCycle,
                CycleType = cycleType
            };

            return ProcessRetailSummaryRequest(request);
        }

        private IActionResult ProcessRetailSummaryRequest(RetailSummaryRequest request)
        {
            try
            {
                if (!_ordSummaryDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _ordSummaryDao.GetRetailSummaryReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessRetailSummaryRequest failed. request={@request}", request);
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get retail summary report data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("retail/summary-bulk")]
        public IActionResult GetRetailBulkSummaryReport([FromUri] string billCycle)
        {
            var validationErrors = new List<string>();

            // Validate bill cycle parameter
            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new RetailSummaryRequest
            {
                BillCycle = billCycle,
                CycleType = "A" // Bulk only uses bill cycle
            };

            return ProcessRetailBulkSummaryRequest(request);
        }

        private IActionResult ProcessRetailBulkSummaryRequest(RetailSummaryRequest request)
        {
            try
            {
                if (!_bulkSummaryDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _bulkSummaryDao.GetRetailBulkSummaryReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessRetailBulkSummaryRequest failed. request={@request}", request);
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get retail bulk summary report data.",
                    errorDetails = ex.Message
                });
            }
        }


        [HttpGet]
        [Route("solarPVCapacity/ordinary")]
        public IActionResult GetPVCapacityOrdinaryReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessPVCapacityOrdinaryRequest(request);
        }

        private IActionResult ProcessPVCapacityOrdinaryRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_pvCapacityOrdinaryDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _pvCapacityOrdinaryDao.GetPVCapacityReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessPVCapacityOrdinaryRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get PV capacity data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("solarPVCapacity/bulk")]
        public IActionResult GetPVCapacityBulkReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessPVCapacityBulkRequest(request);
        }

        private IActionResult ProcessPVCapacityBulkRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_pvCapacityBulkDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _pvCapacityBulkDao.GetPVCapacityBulkReport(request);

                // Check if data is empty due to null generation capacity
                if (data == null || data.Count == 0)
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data available. This may be due to null generation capacity records for the specified bill cycle.",
                        errorDetails = "Please check the data for the bill cycle or contact the administrator."
                    });
                }

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessPVCapacityBulkRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get PV capacity bulk data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("solarPVCapacity/summary")]
        public IActionResult GetPVCapacitySummaryReport(
            [FromUri] string billCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarProgressRequest
            {
                BillCycle = billCycle
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessPVCapacitySummaryRequest(request);
        }

        private IActionResult ProcessPVCapacitySummaryRequest(SolarProgressRequest request)
        {
            try
            {
                if (!_pvCapacitySummaryDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateRequestParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _pvCapacitySummaryDao.GetPVCapacitySummaryReport(request);

                // Check if data is empty due to null generation capacity
                if (data == null || data.Count == 0)
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data available. This may be due to null generation capacity records or no data for the specified criteria.",
                        errorDetails = "Please check the data for the bill cycle or contact the administrator."
                    });
                }

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessPVCapacitySummaryRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get PV capacity summary data.",
                    errorDetails = ex.Message
                });
            }
        }
        [HttpGet]
        [Route("solarPayment/bulk")]
        public IActionResult GetSolarPaymentBulkReport(
            [FromUri] string billCycle,
            [FromUri] string netType = "1",   // Net type filter (1, 2, 3, 4, 5)
            [FromUri] string reportType = "entireceb",
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            // Validate bill cycle parameter
            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            // Validate net type
            if (string.IsNullOrWhiteSpace(netType))
                validationErrors.Add("Net type is required.");
            else if (!new[] { "1", "2", "3", "4", "5" }.Contains(netType))
                validationErrors.Add("Net type must be 1, 2, 3, 4, or 5.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SolarPaymentBulkRequest
            {
                BillCycle = billCycle,
                NetType = netType
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessSolarPaymentBulkRequest(request);
        }

        private IActionResult ProcessSolarPaymentBulkRequest(SolarPaymentBulkRequest request)
        {
            try
            {
                if (!_solarPaymentBulkDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                string typeValidationError = ValidateSolarPaymentBulkParameters(request);
                if (!string.IsNullOrEmpty(typeValidationError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = typeValidationError,
                        errorDetails = "Invalid request parameters."
                    });
                }

                var data = _solarPaymentBulkDao.GetSolarPaymentBulkReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessSolarPaymentBulkRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar payment bulk report data.",
                    errorDetails = ex.Message
                });
            }
        }

        private string ValidateSolarPaymentBulkParameters(SolarPaymentBulkRequest request)
        {
            return ValidateReportTypeParameters(
                request.ReportType,
                request.AreaCode,
                request.ProvCode,
                request.Region
            );
        }

        [HttpGet]
        [Route("solar-rooftop-payments-export-units")]
        public IActionResult GetSolarRooftopPaymentsAndExportUnitsReport(
            [FromUri] string customerType,
            [FromUri] string type,
            [FromUri] string divisionOrProvince = null,
            [FromUri] string billCycle = null,
            [FromUri] string reportType = "payments")
        {
            // Accept requests from frontend versions using the original parameter name.
            // An optional scope parameter lets Web API reach this action for either name.
            if (string.IsNullOrWhiteSpace(divisionOrProvince))
                divisionOrProvince = Request.GetQueryNameValuePairs()
                    .FirstOrDefault(parameter => string.Equals(parameter.Key, "division", StringComparison.OrdinalIgnoreCase))
                    .Value;

            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(customerType))
                validationErrors.Add("Customer type is required.");
            else if (!new[] { "ordinary", "bulk" }.Contains(customerType.Trim(), StringComparer.OrdinalIgnoreCase))
                validationErrors.Add("Customer type must be Ordinary or Bulk.");

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(type))
                validationErrors.Add("Type is required.");
            else if (!new[] { "Province", "Division" }.Contains(type.Trim(), StringComparer.OrdinalIgnoreCase))
                validationErrors.Add("Type must be Province or Division.");

            if (string.IsNullOrWhiteSpace(divisionOrProvince))
                validationErrors.Add("Province or division is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("Report type is required.");
            else if (!new[] { "payments", "exportunits", "export units" }.Contains(reportType.Trim(), StringComparer.OrdinalIgnoreCase))
                validationErrors.Add("Report type must be payments or exportUnits.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            bool isProvince = type.Trim().Equals("Province", StringComparison.OrdinalIgnoreCase);
            string locationCode = divisionOrProvince.Trim();
            // The province dropdown displays "code - name" but filters by code only.
            // Accept that display text from manual Swagger requests as well.
            if (isProvince)
            {
                int separator = locationCode.IndexOf('-');
                if (separator > 0)
                    locationCode = locationCode.Substring(0, separator).Trim();
            }

            var request = new SolarRooftopPaymentsandExportUnitsRequest
            {
                CustomerType = customerType.Trim(),
                BillCycle = billCycle.Trim(),
                Province = isProvince ? locationCode : null,
                Area = null,
                Region = isProvince ? null : locationCode,
                ReportType = reportType.Trim().Equals("exportunits", StringComparison.OrdinalIgnoreCase) ||
                    reportType.Trim().Equals("export units", StringComparison.OrdinalIgnoreCase)
                    ? SolarRooftopReportType.ExportUnits
                    : SolarRooftopReportType.Payments
            };

            try
            {
                if (!_solarRooftopPaymentsandExportUnitsDao.TestConnection(out string connError, request.CustomerType))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _solarRooftopPaymentsandExportUnitsDao.GetReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetSolarRooftopPaymentsAndExportUnitsReport failed. customerType={customerType}, type={type}, divisionOrProvince={divisionOrProvince}, billCycle={billCycle}, reportType={reportType}", customerType, type, divisionOrProvince, billCycle, reportType);
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar rooftop payments and export units report data.",
                    errorDetails = ex.Message
                });
            }
        }


        [HttpGet]
        [Route("solarConnectionDetails/retail/detailed")]
        public IActionResult GetSolarConnectionDetailsRetailDetailedReport(
            [FromUri] string cycleType = "A",     // A = BillCycle, B = CalcCycle
            [FromUri] string billCycle = null,
            [FromUri] string calcCycle = null,
            [FromUri] string netType = "1",
            [FromUri] string reportType = "entireceb",
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            // Validate cycle parameters
            if (cycleType == "A" && string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required when cycle type is 'A'.");
            if (cycleType == "B" && string.IsNullOrWhiteSpace(calcCycle))
                validationErrors.Add("Calc cycle is required when cycle type is 'B'.");

            // Validate net type
            if (string.IsNullOrWhiteSpace(netType))
                validationErrors.Add("Net type is required.");
            else if (!new[] { "1", "2", "3", "4", "5" }.Contains(netType))
                validationErrors.Add("Net type must be 1, 2, 3, 4, or 5.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new RetailDetailedRequest
            {
                CycleType = cycleType,
                BillCycle = billCycle,
                CalcCycle = calcCycle,
                NetType = netType
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessSolarReadingDetailedRequest(request);
        }

        private IActionResult ProcessSolarReadingDetailedRequest(RetailDetailedRequest request)
        {
            try
            {
                if (!_solarReadingDetailedDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _solarReadingDetailedDao.GetSolarReadingDetailedReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessSolarReadingDetailedRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar reading detailed report data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("solarConnectionDetails/retail/summary")]
        public IActionResult GetSolarConnectionDetailsRetailSummaryReport(
            [FromUri] string cycleType = "A",     // A = BillCycle, B = CalcCycle
            [FromUri] string billCycle = null,
            [FromUri] string calcCycle = null,
            [FromUri] string netType = "1",
            [FromUri] string reportType = "entireceb",
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            // Validate cycle parameters
            if (cycleType == "A" && string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required when cycle type is 'A'.");
            if (cycleType == "B" && string.IsNullOrWhiteSpace(calcCycle))
                validationErrors.Add("Calc cycle is required when cycle type is 'B'.");

            // Validate net type
            if (string.IsNullOrWhiteSpace(netType))
                validationErrors.Add("Net type is required.");
            else if (!new[] { "1", "2", "3", "4", "5" }.Contains(netType))
                validationErrors.Add("Net type must be 1, 2, 3, 4, or 5.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new RetailDetailedRequest
            {
                CycleType = cycleType,
                BillCycle = billCycle,
                CalcCycle = calcCycle,
                NetType = netType
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessSolarReadingSummaryRequest(request);
        }

        private IActionResult ProcessSolarReadingSummaryRequest(RetailDetailedRequest request)
        {
            try
            {
                if (!_solarReadingSummaryDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _solarReadingSummaryDao.GetSolarReadingSummaryReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessSolarReadingSummaryRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar reading summary report data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("solarConnectionDetails/bulk")]
        public IActionResult GetSolarConnectionDetailsBulkReport(
            [FromUri] string addedBillCycle = null,
            [FromUri] string billCycle = null,
            [FromUri] string netType = "1",
            [FromUri] string reportType = "entireceb",
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            // Validate required parameters
            if (string.IsNullOrWhiteSpace(addedBillCycle))
                validationErrors.Add("Added bill cycle (added_blcy) is required.");

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            // Validate net type
            if (string.IsNullOrWhiteSpace(netType))
                validationErrors.Add("Net type is required.");
            else if (!new[] { "1", "2", "3", "4", "5" }.Contains(netType))
                validationErrors.Add("Net type must be 1, 2, 3, 4, or 5.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new BulkUsageRequest
            {
                AddedBillCycle = addedBillCycle,
                BillCycle = billCycle,
                NetType = netType
            };

            switch (reportType.ToLower())
            {
                case "area":
                    request.ReportType = SolarReportType.Area;
                    request.AreaCode = typeCode;
                    if (string.IsNullOrWhiteSpace(typeCode))
                    {
                        return Ok(new
                        {
                            data = (object)null,
                            errorMessage = "Area code is required for area report type."
                        });
                    }
                    break;
                case "province":
                    request.ReportType = SolarReportType.Province;
                    request.ProvCode = typeCode;
                    if (string.IsNullOrWhiteSpace(typeCode))
                    {
                        return Ok(new
                        {
                            data = (object)null,
                            errorMessage = "Province code is required for province report type."
                        });
                    }
                    break;
                case "region":
                    request.ReportType = SolarReportType.Region;
                    request.Region = typeCode;
                    if (string.IsNullOrWhiteSpace(typeCode))
                    {
                        return Ok(new
                        {
                            data = (object)null,
                            errorMessage = "Region is required for region report type."
                        });
                    }
                    break;
                case "entireceb":
                    request.ReportType = SolarReportType.EntireCEB;
                    break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid report type.",
                        errorDetails = "Valid types: Area, Province, Region, EntireCEB."
                    });
            }

            return ProcessSolarReadingUsageBulkRequest(request);
        }

        // 3. Add this helper method:

        private IActionResult ProcessSolarReadingUsageBulkRequest(BulkUsageRequest request)
        {
            try
            {
                if (!_solarReadingUsageBulkDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = _solarReadingUsageBulkDao.GetSolarReadingUsageBulkReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "ProcessSolarReadingUsageBulkRequest failed");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get solar reading usage bulk report data.",
                    errorDetails = ex.Message
                });
            }
        }
        [HttpGet]
        [Route("solarCustomerInformation/{accountNumber}")]
        public IActionResult GetSolarCustomerInformation(string accountNumber)
        {
            try
            {
                // Validate account number
                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Account number is required."
                    });
                }

                System.Diagnostics.Trace.WriteLine($"=== Fetching customer information for account: {accountNumber} ===");

                // Step 1: Check if account exists in Ordinary database
                bool isOrdinary = _solarCustomerInforOrdinaryDao.AccountExists(accountNumber);

                if (isOrdinary)
                {
                    System.Diagnostics.Trace.WriteLine($"Account {accountNumber} found in Ordinary database");

                    // Fetch data from Ordinary database
                    var response = _solarCustomerInforOrdinaryDao.GetCustomerInformation(accountNumber);

                    if (response.CustomerInfo == null)
                    {
                        return Ok(new
                        {
                            data = (object)null,
                            errorMessage = response.ErrorMessage ?? "Customer information not found in Ordinary database."
                        });
                    }

                    return Ok(new
                    {
                        data = response,
                        errorMessage = (string)null
                    });
                }
                else
                {
                    // Step 2: Check if account exists in Bulk database
                    System.Diagnostics.Trace.WriteLine($"Account {accountNumber} not found in Ordinary database, checking Bulk...");

                    bool isBulk = _solarCustomerInforBulkDao.AccountExists(accountNumber);

                    if (isBulk)
                    {
                        System.Diagnostics.Trace.WriteLine($"Account {accountNumber} found in Bulk database");

                        // Fetch data from Bulk database
                        var response = _solarCustomerInforBulkDao.GetCustomerInformation(accountNumber);

                        if (response.CustomerInfo == null)
                        {
                            return Ok(new
                            {
                                data = (object)null,
                                errorMessage = response.ErrorMessage ?? "Customer information not found in Bulk database."
                            });
                        }

                        return Ok(new
                        {
                            data = response,
                            errorMessage = (string)null
                        });
                    }
                    else
                    {
                        // Account not found in either database
                        System.Diagnostics.Trace.WriteLine($"Account {accountNumber} not found in any database");

                        return Ok(new
                        {
                            data = (object)null,
                            errorMessage = "Account number not found in Ordinary or Bulk databases.",
                            customerType = "Unknown"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetSolarCustomerInformation failed. accountNumber={accountNumber}", accountNumber);
                System.Diagnostics.Trace.WriteLine($"ERROR in GetCustomerInformation: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Error retrieving customer information.",
                    errorDetails = ex.Message
                });
            }
        }

        // -----------------------------------------------------------------------
        // GET solarapi/rooftop-solar-input/report
        //
        // Query params:
        //   calcCycle  – raw numeric cycle, e.g. "439"
        //   reportType – "area" | "province" | "region" | "entireceb"
        //   typeCode   – area_code / prov_code / region value (omit for entireceb)
        // -----------------------------------------------------------------------
        [HttpGet]
        [Route("rooftop-solar-input/report")]
        public IActionResult GetRoofTopSolarInputReport(
            [FromUri] string calcCycle,
            [FromUri] string reportType,
            [FromUri] string typeCode = null)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(calcCycle))
                validationErrors.Add("calcCycle is required.");

            if (string.IsNullOrWhiteSpace(reportType))
                validationErrors.Add("reportType is required.");

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "area", "province", "region", "entireceb" };

            if (!string.IsNullOrWhiteSpace(reportType) && !allowedTypes.Contains(reportType))
                validationErrors.Add($"Invalid reportType '{reportType}'. Must be area, province, region, or entireceb.");

            if (reportType?.ToLower() != "entireceb" && string.IsNullOrWhiteSpace(typeCode))
                validationErrors.Add("typeCode is required for area / province / region report types.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            try
            {
                var data = _roofTopSolarInputDataDao.GetReport(calcCycle.Trim(), reportType.Trim(), typeCode?.Trim());

                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetRoofTopSolarInputReport failed. calcCycle={calcCycle}, reportType={reportType}, typeCode={typeCode}", calcCycle, reportType, typeCode);
                System.Diagnostics.Trace.WriteLine($"ERROR GetRoofTopSolarInputReport: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Error retrieving Roof Top Solar Input Data report.",
                    errorDetails = ex.Message
                });
            }
        }

        //Solor Liss recon

        [HttpGet]
        [Route("payment-liss-recon")]
        public IActionResult GetPaymentLissReconReport(
            [FromUri] string billCycle,
            [FromUri] string reportCategory,
            [FromUri] string typeCode)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(billCycle))
                validationErrors.Add("Bill cycle is required.");

            if (string.IsNullOrWhiteSpace(reportCategory))
                validationErrors.Add("Report category (Province or Division) is required.");

            if (string.IsNullOrWhiteSpace(typeCode))
                validationErrors.Add("Province/Division code is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            try
            {
                var request = new SolarPaymentLissReconRequest
                {
                    BillCycle = billCycle.Trim(),
                    ReportCategory = reportCategory.Trim(),
                    TypeCode = typeCode.Trim()
                };

                var result = _solarPaymentLissReconDao.GetReconciliationReport(request);
                return Ok(new
                {
                    data = result,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPaymentLissReconReport failed. billCycle={billCycle}, reportCategory={reportCategory}, typeCode={typeCode}", billCycle, reportCategory, typeCode);
                System.Diagnostics.Trace.WriteLine($"ERROR GetPaymentLissReconReport: {ex.Message}");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Error generating Solar Payment & LISS Reconciliation report.",
                    errorDetails = ex.Message
                });
            }
        }


        //SolarJournalsBulkDao//
        [HttpGet]
        [Route("solar-journals/bulk")]
        public IActionResult GetSolarJournalsBulk(
    [FromUri] string billCycle,
    [FromUri] string reportCategory,
    [FromUri] string typeCode = null)
        {
            if (string.IsNullOrWhiteSpace(billCycle))
                return Ok(new { data = (object)null, errorMessage = "Bill cycle is required." });

            if (string.IsNullOrWhiteSpace(reportCategory))
                return Ok(new { data = (object)null, errorMessage = "Report category is required." });

            try
            {
                var request = new SolarJournalsBulkRequest
                {
                    BillCycle = billCycle.Trim(),
                    ReportCategory = reportCategory.Trim(),
                    TypeCode = typeCode?.Trim()
                };

                var result = _solarJournalsBulkDao.GetSolarJournals(request);
                return Ok(new { data = result, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetSolarJournalsBulk failed. billCycle={billCycle}, reportCategory={reportCategory}, typeCode={typeCode}", billCycle, reportCategory, typeCode);
                return Ok(new { data = (object)null, errorMessage = ex.Message });
            }
        }

    }
}


