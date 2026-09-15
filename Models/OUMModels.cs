// Models/OUMModels.cs
using System;
using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class OUMEmployeeModel
    {
        public DateTime AuthDate { get; set; }
        public int OrderId { get; set; }
        public string AcctNumber { get; set; }
        public string BankCode { get; set; }
        public decimal BillAmt { get; set; }
        public decimal TaxAmt { get; set; }
        public decimal TotAmt { get; set; }
        public string AuthCode { get; set; }
        public string CardNo { get; set; }

    }

    public class OUMCrdTempModel
    {
        public int OrderId { get; set; }
        public string AcctNumber { get; set; }
        public string CustName { get; set; }
        public string UserName { get; set; }
        public decimal BillAmt { get; set; }
        public decimal TaxAmt { get; set; }
        public decimal TotAmt { get; set; }
        public string TrStatus { get; set; }
        public string AuthCode { get; set; }
        public DateTime PmntDate { get; set; }
        public DateTime AuthDate { get; set; }
        public string CebRes { get; set; }
        public int SerlNo { get; set; }
        public string BankCode { get; set; }
        public string BranCode { get; set; }
        public string InstStatus { get; set; }
        public string UpdtStatus { get; set; }
        public string UpdtFlag { get; set; }
        public string PostFlag { get; set; }
        public string ErrFlag { get; set; }
        public DateTime? PostDate { get; set; }
        public string CardNo { get; set; }
        public string PaymentType { get; set; }
        public string RefNumber { get; set; }
        public string ReferenceType { get; set; }
        public string SmsSt { get; set; }
        public string ErrorMessage { get; set; }
    }


    public class OUMRequestModel
    {
        public List<OUMEmployeeModel> InsertModel { get; set; }

    }

    public class OUMUploadResponseModel
    {
        public int RecordsInserted { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public List<OUMCrdTempModel> Data { get; set; }
    }

    public class OUMApproveResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }
        public int RecordsProcessed { get; set; }
        public List<OUMCrdTempModel> Data { get; set; }
    }

    public class OUMRecordsResponseModel
    {
        public List<OUMCrdTempModel> Records { get; set; }
        public int TotalRecords { get; set; }
        public string ErrorMessage { get; set; }
        public bool Success { get; set; }
    }
}