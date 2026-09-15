using MISReports_Api.DAL.Collection.ReceivablePosition;
using MISReports_Api.DAL.Collection.SalesAndCollection;
using MISReports_Api.DAL.Shared;
using MISReports_Api.Models.Collection;
using MISReports_Api.DAL.Collection;
using MISReports_Api.Models.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    /// <summary>
    /// Handles collection-related reports.
    /// Exposes endpoints for:
    ///   - Receivable Position (area, by bill cycle and bill type)
    ///   - Sales &amp; Collections – Region Wise (province / region / entire CEB)
    ///   - Receive Position (province / region / entire CEB scope, ordinary + bulk)
    ///
    /// Route prefix: api
    /// </summary>
    [RoutePrefix("api")]
    public class CollectionController : ApiController
    {
        // ── DAO fields ────────────────────────────────────────────────────────
        private readonly ReceivablePositionDao _receivablePositionDao = new ReceivablePositionDao();
        private readonly SalesAndCollectionDao _salesAndCollectionDao = new SalesAndCollectionDao();
        private readonly ReceivablePositionBillCycleDao _salesBillCycleDao = new ReceivablePositionBillCycleDao();
        private readonly ReceivablePositionBillCycleDao _receivePosBillCycleDao = new ReceivablePositionBillCycleDao();
        private readonly SuspensePaymentDetailsDao _suspensePaymentDetailsDao = new SuspensePaymentDetailsDao();
        private readonly ProvinceDao _provinceDao = new ProvinceDao();
        private readonly RegionDao _regionDao = new RegionDao();


        // ================================================================== //
        //  RECEIVABLE POSITION (existing)                                      //
        // ================================================================== //

        [HttpGet]
        [Route("receivable-position/report")]
        public IHttpActionResult GetReceivablePositionReport(
            [FromUri] string billCycle = null,
            [FromUri] string areaCode = null,
            [FromUri] string billType = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(billCycle)) errors.Add("Bill cycle is required.");
            if (string.IsNullOrWhiteSpace(areaCode)) errors.Add("Area code is required.");
            if (string.IsNullOrWhiteSpace(billType)) errors.Add("Bill type is required.");

            if (errors.Count > 0)
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", errors)
                }));

            try
            {
                if (!_receivablePositionDao.TestConnection(out string connError))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));

                var request = new ReceivablePositionRequest
                {
                    BillCycle = billCycle.Trim(),
                    AreaCode = areaCode.Trim(),
                    BillType = billType.Trim().ToUpper()
                };

                var row = _receivablePositionDao.GetReceivablePositionReport(request);

                if (row == null)
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria.",
                        errorDetails = "Please check the bill cycle, area code, and bill type."
                    }));

                return Ok(JObject.FromObject(new
                {
                    data = new List<ReceivablePositionModel> { row },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetReceivablePositionReport: {ex.Message}\n{ex.StackTrace}");

                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get receivable position report data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("receivable-position/areas-by-province")]
        public IHttpActionResult GetReceivablePositionAreasByProvince(
            [FromUri] string provinceCode = null,
            [FromUri] string billType = null)
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Province code is required."
                }));

            try
            {
                if (!_receivablePositionDao.TestConnection(out string connError))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));

                bool isBulk = string.Equals(billType?.Trim(), "B", StringComparison.OrdinalIgnoreCase);
                var data = _receivablePositionDao.GetAreasByProvince(provinceCode.Trim(), isBulk);

                return Ok(JObject.FromObject(new { data, errorMessage = (string)null }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas for province.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("receivable-position/areas-by-region")]
        public IHttpActionResult GetReceivablePositionAreasByRegion(
            [FromUri] string regionCode = null,
            [FromUri] string billType = null)
        {
            if (string.IsNullOrWhiteSpace(regionCode))
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Region code is required."
                }));

            try
            {
                if (!_receivablePositionDao.TestConnection(out string connError))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));

                bool isBulk = string.Equals(billType?.Trim(), "B", StringComparison.OrdinalIgnoreCase);
                var data = _receivablePositionDao.GetAreasByRegion(regionCode.Trim(), isBulk);

                return Ok(JObject.FromObject(new { data, errorMessage = (string)null }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas for region.",
                    errorDetails = ex.Message
                }));
            }
        }

        // NOTE: GetReceivablePositionBillTypes endpoint removed —
        // ReceivablePositionDao has no GetDistinctBillTypes() method.
        // Bill type is a fixed choice in this system: "O" (Ordinary) or "B" (Bulk).
        // The frontend dropdown for Customer Type is hardcoded; no API call needed.


        // ================================================================== //
        //  SALES & COLLECTIONS – REGION WISE (existing)                        //
        // ================================================================== //


        [HttpGet]
        [Route("sales-collection/report")]
        public IHttpActionResult GetSalesCollectionReport(
    [FromUri] string billCycle = null,
    [FromUri] string reportType = "EntireCEB",
    [FromUri] string provinceCode = null,
    [FromUri] string regionCode = null)
        {
            if (string.IsNullOrWhiteSpace(billCycle))
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Bill cycle is required."
                }));
            if (!Enum.TryParse(reportType?.Trim() ?? "EntireCEB", true,
                               out SalesCollectionReportType parsedType))
                parsedType = SalesCollectionReportType.EntireCEB;
            if (parsedType == SalesCollectionReportType.Province &&
                string.IsNullOrWhiteSpace(provinceCode))
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Province code is required for Province report type."
                }));
            if (parsedType == SalesCollectionReportType.Region &&
                string.IsNullOrWhiteSpace(regionCode))
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Region code is required for Region report type."
                }));
            try
            {
                if (!_salesAndCollectionDao.TestConnection(out string connError))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                var request = new SalesAndCollectionRequest
                {
                    BillCycle = billCycle,
                    ReportType = parsedType,
                    ProvinceCode = provinceCode,
                    RegionCode = regionCode
                };
                var data = _salesAndCollectionDao.GetSalesAndCollectionReport(request);
                if (data == null || data.Count == 0)
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "No data found for the selected criteria."
                    }));
                return Ok(JObject.FromObject(new { data, errorMessage = (string)null }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"ERROR GetSalesCollectionReport: {ex.Message}\n{ex.StackTrace}");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get sales and collection report data.",
                    errorDetails = ex.Message
                }));
            }
        }

        // ================================================================== //
        //  HEAD OFFICE POS COLLECTION                                          //
        // ================================================================== //

        [HttpPost]
        [Route("collection/headofficepos")]
        public IHttpActionResult GetHeadOfficePOSReport([FromBody] HeadOfficePOSCollectionRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request cannot be null.");

                if (string.IsNullOrEmpty(request.FromDate) || string.IsNullOrEmpty(request.ToDate))
                    return BadRequest("FromDate and ToDate are required.");

                if (request.ReportType != "Bulk" && request.ReportType != "Ordinary")
                    return BadRequest("Invalid ReportType.");

                var dao = new MISReports_Api.DAL.Collection.HeadOfficePOSCollectionDao();
                var data = dao.GetReportData(request);
                return Ok(new { data, errorMessage = "" });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Failed to fetch report data.", errorDetails = ex.Message });
            }
        }

        // ================================================================== //
        //  CUSTOMERS WITH HIGHEST OUTSTANDING BALANCE (ORDINARY)              //
        // ================================================================== //

        [HttpPost]
        [Route("collection/highest-outstanding")]
        public IHttpActionResult GetHighestOutstandingReport([FromBody] CustomersHighestOutstandingRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest("Request payload cannot be null.");

                if (string.IsNullOrEmpty(request.Scope))
                    return BadRequest("Scope (Province/Division) is required.");

                if (request.Scope.Equals("Province", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(request.ProvinceCode))
                    return BadRequest("ProvinceCode is required when Scope is Province.");

                if (request.Scope.Equals("Division", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(request.RegionCode))
                    return BadRequest("RegionCode (Division) is required when Scope is Division.");

                if (request.MonthsInArrears < 0)
                    return BadRequest("MonthsInArrears must be greater than or equal to 0.");

                if (request.OutstandingBalance < 0)
                    return BadRequest("OutstandingBalance must be greater than or equal to 0.");

                var dao = new MISReports_Api.DAL.Collection.CustomersHighestOutstandingDao();
                // Test connection to the main database first
                if (!dao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Main database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = dao.GetReportData(request);
                return Ok(new
                {
                    data = data,
                    errorMessage = ""
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR CustomersHighestOutstanding GetReport: {ex.Message}\n{ex.StackTrace}");
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Failed to fetch report data.",
                    errorDetails = ex.Message
                });
            }
        }

        [HttpGet]
        [Route("suspense-payment-details")]
        public IHttpActionResult GetSuspensePaymentDetails(
    [FromUri] string fromDate,
    [FromUri] string toDate,
    [FromUri] bool isBulk = true)   // true = Bulk, false = Ordinary (matches the UI's default "Bulk" radio)
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(fromDate))
                validationErrors.Add("From date is required.");

            if (string.IsNullOrWhiteSpace(toDate))
                validationErrors.Add("To date is required.");

            if (validationErrors.Count > 0)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = string.Join("; ", validationErrors)
                });
            }

            var request = new SuspensePaymentDetailsRequest
            {
                FromDate = fromDate,
                ToDate = toDate,
                IsBulk = isBulk
            };

            return ProcessSuspensePaymentDetailsRequest(request);
        }

        private IHttpActionResult ProcessSuspensePaymentDetailsRequest(SuspensePaymentDetailsRequest request)
        {
            try
            {
                if (!_suspensePaymentDetailsDao.TestConnection(out string connError))
                {
                    return Ok(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    });
                }

                var data = request.IsBulk
                    ? _suspensePaymentDetailsDao.GetBulkReport(request)
                    : _suspensePaymentDetailsDao.GetOrdinaryReport(request);

                return Ok(new
                {
                    data = data,
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get suspense payment details data.",
                    errorDetails = ex.Message
                });
            }
        }
    }
}