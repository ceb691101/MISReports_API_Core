using System;

namespace MISReports_Api.Models.PIV
{
    public class BankPivTabulationModel
    {
        public string C6 { get; set; }              // dept_id
        public string PivNo { get; set; }
        public string PivReceiptNo { get; set; }
        public DateTime? PivDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string ChequeNo { get; set; }
        public decimal? GrandTotal { get; set; }
        public string C8 { get; set; }              // account_code
        public decimal? Amount { get; set; }
        public string BankCheckNo { get; set; }
        public string PaymentMode { get; set; }     // Translated value
        public string CctName { get; set; }         // dept_nm
    }
}