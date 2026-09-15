// Controllers/OUMController.cs
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/oum")]
    public class OUMController : ApiController
    {
        private readonly OUMRepository _oumRepository = new OUMRepository();

        [HttpPost]
        [Route("upload")]
        public IHttpActionResult UploadExcelFile(OUMRequestModel OUMModel)
        {
            try
            {

                if (OUMModel.InsertModel.Count == 0)
                {
                    return Ok(JObject.FromObject(new OUMUploadResponseModel
                    {
                        RecordsInserted = 0,
                        Message = null,
                        ErrorMessage = "No valid data found in the Excel file.",
                        Data = null
                    }));
                }

                // Insert into database
                var insertedRows = _oumRepository.InsertIntoAmex2(OUMModel.InsertModel);

                // Refresh CrdTemp table
                _oumRepository.RefreshCrdTemp();

                // Get updated records
                var records = _oumRepository.GetCrdTempRecords();

                var response = new OUMUploadResponseModel
                {
                    RecordsInserted = insertedRows,
                    Message = insertedRows == 1 ? $"Successfully inserted {insertedRows} record" : $"Successfully inserted {insertedRows} records",
                    ErrorMessage = null,
                    Data = records
                };

                return Ok(JObject.FromObject(response));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new OUMUploadResponseModel
                {
                    RecordsInserted = 0,
                    Message = null,
                    ErrorMessage = "Error processing file: " + ex.Message,
                    Data = null
                }));
            }
        }

        [HttpGet]
        [Route("records")]
        public JObject GetRecords()
        {
            OUMRecordsResponseModel response = new OUMRecordsResponseModel();
            try
            {
                var records = _oumRepository.GetCrdTempRecords();
                response.Records = records;
            }
            catch (Exception ex)
            {
                response.ErrorMessage = "error";
                response.TotalRecords = 0;
            }

            return JObject.Parse(JsonConvert.SerializeObject(response));
        }

        [HttpGet]
        [Route("approveload")]
        public IHttpActionResult ApproveRecordsLoad()
        {
            try
            {

                var records = _oumRepository.GetCrdTempRecords();
                // var success = _oumRepository.ApproveRecords();

                var response = new OUMApproveResponseModel
                {
                    Success = true,
                    Message = "Success",
                    ErrorMessage = null,
                    // RecordsProcessed = success ? _oumRepository.GetCrdTempRecords().Count : 0,
                    Data = records
                };

                return Ok(JObject.FromObject(new
                {
                    data = response,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {

                return Ok(JObject.FromObject(new
                {
                    data = new OUMApproveResponseModel
                    {
                        Success = false,
                        Message = "Transaction failed",
                        ErrorMessage = ex.Message,
                        RecordsProcessed = 0
                    },
                    errorMessage = "Cannot approve OUM records.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("approve")]
        public IHttpActionResult ApproveRecords()
        {
            try
            {
                var success = _oumRepository.ApproveRecords();
                var records = _oumRepository.GetCrdTempRecords();

                var response = new OUMRecordsResponseModel
                {
                    Success = success,
                    //Message = success ? "Records successfully approved and moved to production." : "Failed to approve records.",
                    ErrorMessage = null,
                     //RecordsProcessed = success ? records.Count : 0,
                    Records = records
                };

                return Ok(response);
            }

            catch (Exception ex)
            {
                var response = new OUMRecordsResponseModel
                {
                    Success = false,
                    //Message =  "Failed to approve records.",
                    ErrorMessage = null,
                    // RecordsProcessed =  0,
                    Records = null
                };

                return Ok(response);
            }
        }

        [HttpPost]
        [Route("refresh")]
        public IHttpActionResult RefreshCrdTemp()
        {
            try
            {
                _oumRepository.RefreshCrdTemp();
                var records = _oumRepository.GetCrdTempRecords();

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        message = "CrdTemp table refreshed successfully",
                        recordCount = records.Count,
                        records = records 
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot refresh CrdTemp table.",
                    errorDetails = ex.Message
                }));
            }
        }


        public class Employee
        {
            public DateTime auth_date { get; set; }
            public int order_id { get; set; }
            public string acct_number { get; set; }
            public string bank_code { get; set; }
            public decimal bill_amt { get; set; }
            public decimal tax_amt { get; set; }
            public decimal tot_amt { get; set; }
            public string auth_code { get; set; }
            public string card_no { get; set; }
        }

        public class CrdTemp
        {
            public int order_id { get; set; }
            public string acct_number { get; set; }
            public string custname { get; set; }
            public string username { get; set; }
            public decimal bill_amt { get; set; }
            public decimal tax_amt { get; set; }
            public decimal tot_amt { get; set; }
            public string trstatus { get; set; }
            public string authcode { get; set; }
            public DateTime pmnt_date { get; set; }
            public DateTime auth_date { get; set; }
            public string cebres { get; set; }
            public int serl_no { get; set; }
            public string bank_code { get; set; }
            public string bran_code { get; set; }
            public string inst_status { get; set; }
            public string updt_status { get; set; }
            public string updt_flag { get; set; }
            public string post_flag { get; set; }
            public string err_flag { get; set; }
            public DateTime post_date { get; set; }
            public string card_no { get; set; }
            public string payment_type { get; set; }
            public string ref_number { get; set; }
            public string reference_type { get; set; }
            public string sms_st { get; set; }
        }
    }
}