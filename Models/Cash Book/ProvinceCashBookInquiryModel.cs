using System;
namespace MISReports_Api.Models.Accounts
{
    public class ProvinceCashBookInquiryModel
    {
        public string Category { get; set; }
        public string DeptId { get; set; }
        public DateTime? DocDt { get; set; }
        public decimal? NonTaxabl { get; set; }
        public string DocNo { get; set; }
        public string ApprvUid1 { get; set; }
        public string ApprDt1 { get; set; }
        public string TranStatus { get; set; }
        public string Payee { get; set; }
        public string ChqDt { get; set; }
        public string ChqNo { get; set; }
        public string PymtDocNo { get; set; }
        public string PpStatus { get; set; }
        public string BranchName { get; set; }
    }
}