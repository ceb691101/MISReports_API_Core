using MISReports_Api.Compat;
using System;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL.CustomerDetails;
using MISReports_Api.Models.CustomerDetails;

namespace MISReports_Api.Controllers
{
    //Request Entry
    [RoutePrefix("api/customerdetails")]
    public class CustomerDetailsController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PaymentInquiryDao _paymentInquiryDao;

        public CustomerDetailsController()
        {
            _paymentInquiryDao = new PaymentInquiryDao();
        }

        /// <summary>GET api/customerdetails/latest-update-times</summary> 
        /// Latest Update Times of Servers
        [HttpGet]
        [Route("latest-update-times")]
        public IActionResult GetLatestUpdateTimes()
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetLatestUpdateTimes();

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetLatestUpdateTimes failed");
                return Ok(new { data = (object)null, errorMessage = "Cannot get latest update times.", errorDetails = ex.Message });
            }
        }

        /// <summary>POST api/customerdetails/payment-full-report</summary> 
        /// Individual Payments
        [HttpPost]
        [Route("payment-full-report")]
        public IActionResult GetPaymentFullReport([FromBody] PaymentInquiryRequest request)
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetFullReport(request);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPaymentFullReport failed. request={@request}", request);
                return Ok(new { data = (object)null, errorMessage = "Cannot get payment full report.", errorDetails = ex.Message });
            }
        }

        #region POS Counter Collection Breakup

        /// <summary>GET api/customerdetails/pos-provinces</summary>
        [HttpGet]
        [Route("pos-provinces")]
        public IActionResult GetProvinces()
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetProvinces();

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetProvinces failed");
                return Ok(new { data = (object)null, errorMessage = "Cannot get provinces.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/customerdetails/pos-areas?provCode={provCode}</summary>
        [HttpGet]
        [Route("pos-areas")]
        public IActionResult GetAreas([FromUri] string provCode)
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetAreas(provCode);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAreas failed. provCode={provCode}", provCode);
                return Ok(new { data = (object)null, errorMessage = "Cannot get areas.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/customerdetails/pos-counters?provCode={provCode}</summary>
        [HttpGet]
        [Route("pos-counters")]
        public IActionResult GetCounters([FromUri] string provCode)
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetCounters(provCode);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetCounters failed. provCode={provCode}", provCode);
                return Ok(new { data = (object)null, errorMessage = "Cannot get counters.", errorDetails = ex.Message });
            }
        }

        /// <summary>POST api/customerdetails/pos-collection-breakup</summary>
        [HttpPost]
        [Route("pos-collection-breakup")]
        public IActionResult GetPOSCollectionBreakup([FromBody] POSCounterCollectionRequest request)
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetPOSCounterCollectionBreakup(request);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPOSCollectionBreakup failed. request={@request}", request);
                return Ok(new { data = (object)null, errorMessage = "Cannot get POS counter collection breakup.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/customerdetails/pos-pay-modes</summary>
        [HttpGet]
        [Route("pos-pay-modes")]
        public IActionResult GetPayModes()
        {
            try
            {
                if (!_paymentInquiryDao.TestConnection(out string connError))
                {
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });
                }

                var data = _paymentInquiryDao.GetPayModes();

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetPayModes failed");
                return Ok(new { data = (object)null, errorMessage = "Cannot get pay modes.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/customerdetails/pos-bill-types</summary>
        [HttpGet]
        [Route("pos-bill-types")]
        public IActionResult GetBillTypes()
        {
            try
            {
                var data = _paymentInquiryDao.GetBillTypes();

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetBillTypes failed");
                return Ok(new { data = (object)null, errorMessage = "Cannot get bill types.", errorDetails = ex.Message });
            }
        }

        #endregion

        /// <summary>POST api/customerdetails/standing-order-report</summary>
        /// Standing Order (Automatic Bill Settlement Inquiry)
        [HttpPost]
        [Route("standing-order-report")]
        public IActionResult GetStandingOrderReport([FromBody] StandingOrderRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.AcctNo))
                {
                    return BadRequest("Account number is required.");
                }

                var dao = new StandingOrderDao();
                var result = dao.GetStandingOrderDetails(request.AcctNo);

                return Ok(new
                {
                    data = result,
                    errorMessage = string.IsNullOrWhiteSpace(result.ErrorMessage) ? (string)null : result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetStandingOrderReport failed. request={@request}", request);
                return Ok(new { data = (object)null, errorMessage = "Cannot get standing order report.", errorDetails = ex.Message });
            }
        }
    }
}
