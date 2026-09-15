using System;
namespace MISReports_Api.Models.Accounts
{
    public class TenderDocInquiryModel
    {
        public string DocNo { get; set; }
        public string Payee { get; set; }
        public decimal? NonTaxabl { get; set; }
        public string Remarks { get; set; }
        public string ChqNo { get; set; }
        public DateTime? ChqDt { get; set; }
        public decimal? ChqAmt { get; set; }
        public string Ref4 { get; set; }
        public string BranchName { get; set; }
    }
}