//01.Branch/Province wise PIV Collections Paid to Bank

// File: ProvincePIVbankModel.cs
using System;

namespace MISReports_Api.Models
{
    public class ProvincePIVbankModel
    {
        public string CostCenter { get; set; }
        public string PivNo { get; set; }
        public string PivReceiptNo { get; set; }
        public DateTime? PivDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string ChequeNo { get; set; }
        public decimal GrandTotal { get; set; }
        public string AccountCode { get; set; }
        public decimal Amount { get; set; }
        public string BankCheckNo { get; set; }
        public string PaymentMode { get; set; }
        public string CCT_NAME { get; set; }
        public string COMPANY_NAME { get; set; }
    }
}