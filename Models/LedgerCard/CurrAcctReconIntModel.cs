using System;

namespace MISReports_Api.Models
{
    public class CurrAcctReconIntModel
    {
        public DateTime? EntDt { get; set; }
        public DateTime? PostDt { get; set; }
        public DateTime? AcctDt { get; set; }
        public string SubAc { get; set; }
        public string DeptId1 { get; set; }
        public string DeptId { get; set; }
        public string ParentId { get; set; }
        public string TrfParentId { get; set; }
        public string DocPf { get; set; }
        public string DocNo { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Remarks { get; set; }
        public string DesgDept { get; set; }
        public decimal? CrAmt { get; set; }
        public decimal? DrAmt { get; set; }
        public int? LogMth { get; set; }
        public decimal? OpBal { get; set; }
        public decimal? ClBal { get; set; }
        public string CompNm { get; set; }
    }
}
