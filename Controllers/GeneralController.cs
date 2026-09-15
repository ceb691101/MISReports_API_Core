using MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff;
using MISReports_Api.DAL.General.SecurityDepositContractDemandBulk;
using MISReports_Api.DAL.General.ListOfGovernmentAccounts;
using MISReports_Api.DAL.General.ListingOfCustomer;
using MISReports_Api.DAL.General.ArrearsPosition;
using MISReports_Api.DAL.Dashboard;
using MISReports_Api.DAL.Shared;
using MISReports_Api.DAL;
using MISReports_Api.DAL.Collection;
using MISReports_Api.Models;
using MISReports_Api.Models.General;
using MISReports_Api.Models.SolarInformation;
using MISReports_Api.Models.Shared;
using MISReports_Api.Models.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace MISReports_Api.Controllers
{
    /// <summary>
    /// Handles general / cross-cutting reports.
    /// Exposes endpoints for:
    ///   - Contract Demand Bulk (area / province)
    ///   - Active Customers Ordinary and Bulk (area / province / region / entireceb)
    ///   - Sales by Tariff Ordinary and Bulk (area / province / region / entireceb)
    ///   - Sales and Collection Range
    ///   - SMS Registered Range
    ///   - Government Accounts (areas / departments / area / department)
    ///   - Areas Position
    ///   - Listing of Customers (area, with optional filters)
    ///   - Finalized Accounts (dropdowns / report)
    ///
    /// Route prefix: api
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api")]
    public class GeneralController : ApiController
    {
        // ── DAO fields ────────────────────────────────────────────────────────
        private readonly ContractDemandBulkDao _contractDemandBulkDao = new ContractDemandBulkDao();
        private readonly ContractDemandBillCycleDao _billCycleDao = new ContractDemandBillCycleDao();
        private readonly SalesAndCollectionRangeDao _dao = new SalesAndCollectionRangeDao();
        private readonly RegisteredCustomersBillCycleDao _smsDao = new RegisteredCustomersBillCycleDao();

        private readonly ActiveCustomersOrdinaryDao _activeCustomersOrdinaryDao = new ActiveCustomersOrdinaryDao();
        private readonly ActiveCustomersBulkDao _activeCustomersBulkDao = new ActiveCustomersBulkDao();

        private readonly SalesByTariffOrdinaryDao _salesByTariffOrdinaryDao = new SalesByTariffOrdinaryDao();
        private readonly SalesByTariffBulkDao _salesByTariffBulkDao = new SalesByTariffBulkDao();

        private readonly GovernmentAccountsDao _govAccountsDao = new GovernmentAccountsDao();
        private readonly ListingOfCustomerDao _listingOfCustomerDao = new ListingOfCustomerDao();
        private readonly ArrearsPositionDao _arrearsPositionDao = new ArrearsPositionDao();

        private readonly FinalizedAccountsDao _finalizedAccountsDao = new FinalizedAccountsDao();


        // ================================================================== //
        //  CONTRACT DEMAND BULK                                                //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/contract-demand/bulk/area?billCycle=438&areaCode=43        //
        // Response: { data: [ ...CustomerRecords... ], errorMessage: null }  //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bulk/area")]
        public IHttpActionResult GetContractDemandAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessContractDemandRequest(new SecDepositConDemandRequest
            {
                BillCycle = billCycle,
                ReportType = SolarReportType.Area,
                AreaCode = areaCode
            });
        }

        // ------------------------------------------------------------------ //
        // GET api/contract-demand/bulk/province?billCycle=438&provCode=D     //
        // Response: { data: [ ...CustomerRecords... ], errorMessage: null }  //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("contract-demand/bulk/province")]
        public IHttpActionResult GetContractDemandProvinceReport(
            [FromUri] string billCycle = null,
            [FromUri] string provCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(provCode)) errors.Add("Province code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessContractDemandRequest(new SecDepositConDemandRequest
            {
                BillCycle = billCycle,
                ReportType = SolarReportType.Province,
                ProvCode = provCode
            });
        }

        private IHttpActionResult ProcessContractDemandRequest(SecDepositConDemandRequest request)
        {
            try
            {
                if (!_contractDemandBulkDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _contractDemandBulkDao.GetContractDemandBulkReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle and location code."
                    });

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR ProcessContractDemandRequest: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get report data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  ACTIVE CUSTOMERS                                                    //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/activeCustomers/ordinary                                    //
        //     ?fromCycle=2401A&toCycle=2406A                                  //
        //     &reportType=area|province|region|entireceb                      //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("activeCustomers/ordinary")]
        public IHttpActionResult GetActiveCustomersOrdinary(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new ActiveCustomersRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area": request.ReportType = ActiveCustomersReportType.Area; break;
                case "province": request.ReportType = ActiveCustomersReportType.Province; break;
                case "region": request.ReportType = ActiveCustomersReportType.Region; break;
                case "entireceb": request.ReportType = ActiveCustomersReportType.EntireCEB; break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessActiveCustomersOrdinaryRequest(request);
        }

        // ------------------------------------------------------------------ //
        // GET api/activeCustomers/bulk                                        //
        //     ?fromCycle=2401&toCycle=2406                                    //
        //     &reportType=area|province|region|entireceb                      //
        // TM1 is always excluded by the DAO layer.                           //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("activeCustomers/bulk")]
        public IHttpActionResult GetActiveCustomersBulk(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new ActiveCustomersRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area": request.ReportType = ActiveCustomersReportType.Area; break;
                case "province": request.ReportType = ActiveCustomersReportType.Province; break;
                case "region": request.ReportType = ActiveCustomersReportType.Region; break;
                case "entireceb": request.ReportType = ActiveCustomersReportType.EntireCEB; break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessActiveCustomersBulkRequest(request);
        }

        private IHttpActionResult ProcessActiveCustomersOrdinaryRequest(ActiveCustomersRequest request)
        {
            try
            {
                if (!_activeCustomersOrdinaryDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Ordinary database connection failed.",
                        errorDetails = connError
                    });

                var data = _activeCustomersOrdinaryDao.GetActiveCustomersOrdinaryReport(request);
                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve active customers (ordinary) report data.",
                    errorDetails = ex.Message
                });
            }
        }

        private IHttpActionResult ProcessActiveCustomersBulkRequest(ActiveCustomersRequest request)
        {
            try
            {
                if (!_activeCustomersBulkDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Bulk database connection failed.",
                        errorDetails = connError
                    });

                var data = _activeCustomersBulkDao.GetActiveCustomersBulkReport(request);
                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve active customers (bulk) report data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  SMS REGISTERED RANGE                                                //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/original/smsRegisteredRange                                 //
        //     ?fromCycle=...&toCycle=...&reportType=...&typeCode=...          //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("original/smsRegisteredRange")]
        public IHttpActionResult GetSMSRegisteredRange(
            [FromUri] string fromCycle = null,
            [FromUri] string toCycle = null,
            [FromUri] string reportType = null,
            [FromUri] string typeCode = null)
        {
            try
            {
                var validationErrors = new List<string>();
                if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("From bill cycle is required.");
                if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("To bill cycle is required.");
                if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("Report type is required.");

                if (validationErrors.Count > 0)
                    return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

                var request = new SMSUsageRequest
                {
                    FromBillCycle = fromCycle,
                    ToBillCycle = toCycle,
                    ReportType = reportType,
                    TypeCode = typeCode
                };

                var monthlyData = _smsDao.GetSMSCountRange(request);

                if (monthlyData == null || monthlyData.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data available for the specified criteria.",
                        errorDetails = "Please check the bill cycle range and location code."
                    });

                return Ok(new
                {
                    data = new Models.SMSRegisteredCustomersModel
                    {
                        LocationName = string.IsNullOrEmpty(typeCode) ? "Entire CEB" : typeCode,
                        MonthlyCounts = monthlyData
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in GetSMSRegisteredRange: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve SMS registered range data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  SALES BY TARIFF                                                     //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/salesByTariff/ordinary                                      //
        //     ?fromCycle=439&toCycle=449                                       //
        //     &reportType=area|province|region|entireceb                      //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("salesByTariff/ordinary")]
        public IHttpActionResult GetSalesByTariffOrdinary(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SalesByTariffRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area": request.ReportType = SalesByTariffReportType.Area; break;
                case "province": request.ReportType = SalesByTariffReportType.Province; break;
                case "region": request.ReportType = SalesByTariffReportType.Region; break;
                case "entireceb": request.ReportType = SalesByTariffReportType.EntireCEB; break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessSalesByTariffOrdinaryRequest(request);
        }

        // ------------------------------------------------------------------ //
        // GET api/salesByTariff/bulk                                          //
        //     ?fromCycle=439&toCycle=449                                       //
        //     &reportType=area|province|region|entireceb                      //
        // TM1 is always excluded. Aggregated field: KwhSales (sum kwh_units). //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("salesByTariff/bulk")]
        public IHttpActionResult GetSalesByTariffBulk(
            [FromUri] string fromCycle,
            [FromUri] string toCycle,
            [FromUri] string reportType)
        {
            var validationErrors = new List<string>();
            if (string.IsNullOrWhiteSpace(fromCycle)) validationErrors.Add("fromCycle is required.");
            if (string.IsNullOrWhiteSpace(toCycle)) validationErrors.Add("toCycle is required.");
            if (string.IsNullOrWhiteSpace(reportType)) validationErrors.Add("reportType is required.");

            if (validationErrors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", validationErrors) });

            var request = new SalesByTariffRequest
            {
                FromCycle = fromCycle.Trim(),
                ToCycle = toCycle.Trim()
            };

            switch (reportType.Trim().ToLower())
            {
                case "area": request.ReportType = SalesByTariffReportType.Area; break;
                case "province": request.ReportType = SalesByTariffReportType.Province; break;
                case "region": request.ReportType = SalesByTariffReportType.Region; break;
                case "entireceb": request.ReportType = SalesByTariffReportType.EntireCEB; break;
                default:
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Invalid reportType.",
                        errorDetails = "Valid values: area, province, region, entireceb."
                    });
            }

            return ProcessSalesByTariffBulkRequest(request);
        }

        private IHttpActionResult ProcessSalesByTariffOrdinaryRequest(SalesByTariffRequest request)
        {
            try
            {
                if (!_salesByTariffOrdinaryDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Ordinary database connection failed.",
                        errorDetails = connError
                    });

                var data = _salesByTariffOrdinaryDao.GetSalesByTariffOrdinaryReport(request);
                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve sales by tariff (ordinary) report data.",
                    errorDetails = ex.Message
                });
            }
        }

        private IHttpActionResult ProcessSalesByTariffBulkRequest(SalesByTariffRequest request)
        {
            try
            {
                if (!_salesByTariffBulkDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Bulk database connection failed.",
                        errorDetails = connError
                    });

                var data = _salesByTariffBulkDao.GetSalesByTariffBulkReport(request);
                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve sales by tariff (bulk) report data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  GOVERNMENT ACCOUNTS                                                 //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/government-accounts/departments                             //
        // Response: { data: [ { departmentCode, departmentName }, ... ],     //
        //             errorMessage: null }                                    //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/departments")]
        public IHttpActionResult GetDepartments()
        {
            try
            {
                if (!_govAccountsDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _govAccountsDao.GetDepartments();

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No departments found.",
                        errorDetails = "The department table may be empty."
                    });

                return Ok(new { data = data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetDepartments: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot retrieve departments.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // GET api/government-accounts/area                                    //
        //     ?billCycle=438&areaCode=43                                      //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/area")]
        public IHttpActionResult GetGovernmentAccountsAreaReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessGovernmentAccountsRequest(new GovernmentAccountsRequest
            {
                BillCycle = billCycle.Trim(),
                ReportType = "area",
                AreaCode = areaCode.Trim()
            });
        }

        // ------------------------------------------------------------------ //
        // GET api/government-accounts/department                             //
        //     ?billCycle=438&areaCode=43&departmentCode=ABC                  //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("government-accounts/department")]
        public IHttpActionResult GetGovernmentAccountsDepartmentReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null,
            [FromUri] string departmentCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");
            if (string.IsNullOrWhiteSpace(departmentCode)) errors.Add("Department code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            return ProcessGovernmentAccountsRequest(new GovernmentAccountsRequest
            {
                BillCycle = billCycle.Trim(),
                ReportType = "department",
                AreaCode = areaCode.Trim(),
                DepartmentCode = departmentCode.Trim()
            });
        }

        private IHttpActionResult ProcessGovernmentAccountsRequest(GovernmentAccountsRequest request)
        {
            try
            {
                if (!_govAccountsDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _govAccountsDao.GetGovernmentAccountsReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle, area code, and department code."
                    });

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR ProcessGovernmentAccountsRequest: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get government accounts report data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  LISTING OF CUSTOMERS                                                //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/listing-of-customers/filters?areaCode=43&billCycle=438     //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("listing-of-customers/filters")]
        public IHttpActionResult GetListingOfCustomersFilters(
            [FromUri] string areaCode = null,
            [FromUri] string billCycle = null)
        {
            try
            {
                var errors = new List<string>();
                if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");
                if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");

                if (errors.Count > 0)
                    return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

                if (!_listingOfCustomerDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var filters = _listingOfCustomerDao.GetFilters(areaCode, billCycle);

                if (!string.IsNullOrEmpty(filters.ErrorMessage))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Error loading filter options.",
                        errorDetails = filters.ErrorMessage
                    });

                return Ok(new
                {
                    data = filters,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetListingOfCustomersFilters: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot load filter options.",
                    errorDetails = ex.Message
                });
            }
        }

        // ------------------------------------------------------------------ //
        // POST api/listing-of-customers/report                                //
        // ------------------------------------------------------------------ //

        [HttpPost]
        [Route("listing-of-customers/report")]
        public IHttpActionResult GetListingOfCustomersReport()
        {
            ListingOfCustomerRequest request;

            try
            {
                var bodyJson = Request.Content.ReadAsStringAsync().Result;

                if (string.IsNullOrWhiteSpace(bodyJson))
                    return Ok(new { data = (object)null, errorMessage = "Request body is required." });

                request = Newtonsoft.Json.JsonConvert.DeserializeObject<ListingOfCustomerRequest>(bodyJson);

                if (request == null)
                    return Ok(new { data = (object)null, errorMessage = "Request body could not be parsed." });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Invalid JSON in request body.",
                    errorDetails = ex.Message
                });
            }

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(request.AreaCode)) errors.Add("Area code is required.");
            if (string.IsNullOrWhiteSpace(request.BillCycle)) errors.Add("Bill cycle is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            request.AreaCode = request.AreaCode.Trim();
            request.BillCycle = request.BillCycle.Trim();

            return ProcessListingOfCustomersRequest(request);
        }

        private IHttpActionResult ProcessListingOfCustomersRequest(ListingOfCustomerRequest request)
        {
            try
            {
                if (!_listingOfCustomerDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var data = _listingOfCustomerDao.GetListingOfCustomerReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle, area code, and filter values."
                    });

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR ProcessListingOfCustomersRequest: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get listing of customers report data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  ARREARS POSITION – METER READER WISE                                //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/arrears-position/report?billCycle=438&areaCode=43          //
        // Response: { data: [ ...ArrearsPositionModel... ], errorMessage: null }
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("arrears-position/report")]
        public IHttpActionResult GetArrearsPositionReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");

            if (errors.Count > 0)
                return Ok(new { data = (object)null, errorMessage = string.Join("; ", errors) });

            try
            {
                if (!_arrearsPositionDao.TestConnection(out string connError))
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });

                var request = new ArrearsPositionRequest
                {
                    BillCycle = billCycle.Trim(),
                    AreaCode = areaCode.Trim()
                };

                var data = _arrearsPositionDao.GetArrearsPositionReport(request);

                if (data == null || data.Count == 0)
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle and area code."
                    });

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetArrearsPositionReport: {ex.Message}\n{ex.StackTrace}");

                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get arrears position report data.",
                    errorDetails = ex.Message
                });
            }
        }


        // ================================================================== //
        //  FINALIZED ACCOUNTS                                                  //
        // ================================================================== //

        // ------------------------------------------------------------------ //
        // GET api/FinalizedAccounts/dropdowns?provCode=D                     //
        // Response: FinalizedAccountsDropdownsResult                         //
        // ------------------------------------------------------------------ //

        [HttpGet]
        [Route("FinalizedAccounts/dropdowns")]
        public IHttpActionResult GetFinalizedAccountsDropdowns([FromUri] string provCode = null)
        {
            var result = _finalizedAccountsDao.GetDropdowns(provCode);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
                return InternalServerError(new Exception(result.ErrorMessage));

            return Ok(result);
        }

        // ------------------------------------------------------------------ //
        // POST api/FinalizedAccounts/report                                   //
        // Body: FinalizedAccountsRequest                                      //
        // ------------------------------------------------------------------ //

        [HttpPost]
        [Route("FinalizedAccounts/report")]
        public IHttpActionResult GetFinalizedAccountsReport([FromBody] FinalizedAccountsRequest request)
        {
            if (request == null)
                return BadRequest("Request payload is required.");

            var result = _finalizedAccountsDao.GetReport(request);
            if (!string.IsNullOrEmpty(result.ErrorMessage))
                return InternalServerError(new Exception(result.ErrorMessage));

            return Ok(result);
        }
    }
}