using System;
namespace MISReports_Api.Models.Accounts
{
    public class GlInquiryByDocModel
    {
        public string DocNo { get; set; }
        public string DeptId { get; set; }
        public string DocPf { get; set; }
        public DateTime? DocDt { get; set; }
        public string EntBy { get; set; }
        public string ModiBy { get; set; }
        public string ApprBy { get; set; }
        public decimal? DrAmt { get; set; }
        public decimal? CrAmt { get; set; }
        public string GlCd { get; set; }
        public string SubAc { get; set; }
        public string Remarks { get; set; }
        public decimal? TrxVal { get; set; }
        public string TranStatus { get; set; }
    }
}