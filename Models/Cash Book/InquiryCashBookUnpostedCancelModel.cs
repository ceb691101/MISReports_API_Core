using System;
namespace MISReports_Api.Models.Accounts
{
    public class InquiryCashBookUnpostedCancelModel
    {
        public string DocNo { get; set; }
        public DateTime? DocDt { get; set; }
        public decimal? NonTaxabl { get; set; }
        public string EntBy { get; set; }
        public string ApprvUid1 { get; set; }
        public string TranStatus { get; set; }
        public string RejBy { get; set; }
        public DateTime? RejcDt { get; set; }
        public string CancelledUser { get; set; }
        public DateTime? CancelDt { get; set; }
        public string BranchName { get; set; }
    }
}