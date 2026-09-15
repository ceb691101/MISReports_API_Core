using System;
namespace MISReports_Api.Models.Accounts
{
    public class DocInquiryGlModel
    {
        public string DocPf { get; set; }
        public string DocNo { get; set; }
        public DateTime? DocDt { get; set; }
        public string GlCd { get; set; }
        public decimal? DrAmt { get; set; }
        public decimal? CrAmt { get; set; }
        public decimal? TrxVal { get; set; }
        public string Remarks { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string TrfDept { get; set; }
        public string BranchName { get; set; }
    }
}