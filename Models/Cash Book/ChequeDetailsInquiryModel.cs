using System;
namespace MISReports_Api.Models.Accounts
{
    public class ChequeDetailsInquiryModel
    {
        public string ChqRun { get; set; }
        public DateTime? ChqDt { get; set; }
        public string Payee { get; set; }
        public string PymtDocNo { get; set; }
        public string ChqNo { get; set; }
        public string RunBy { get; set; }
        public string ModiBy { get; set; }
        public string ApprvUid1 { get; set; }
        public string TranStatus { get; set; }
        public string BranchName { get; set; }
    }
}