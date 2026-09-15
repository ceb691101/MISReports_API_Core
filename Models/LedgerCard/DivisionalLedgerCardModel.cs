using System;

namespace MISReports_Api.Models
{
    public class DivisionalLedgerCardModel
    {
        public string SubAc { get; set; }      // NVL(T4.AC_NM, T1.SUB_AC)
        public string GlCd { get; set; }
        public string DocPf { get; set; }
        public string DocNo { get; set; }
        public decimal? CrAmt { get; set; }
        public decimal? DrAmt { get; set; }
        public string Remarks { get; set; }
        public DateTime? AcctDt { get; set; }
        public int? LogMth { get; set; }
        public string ChqNo { get; set; }
        public string Ref1 { get; set; }
        public decimal? OpBal { get; set; }
        public decimal? ClBal { get; set; }
    }
}