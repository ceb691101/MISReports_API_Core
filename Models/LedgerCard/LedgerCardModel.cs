using System;

namespace MISReports_Api.Models
{
    public class LedgerCardModel
    {
        public string GlCd { get; set; }
        public string SubAc { get; set; }
        public string Remarks { get; set; }
        public DateTime? AcctDt { get; set; }
        public string DocPf { get; set; }
        public string DocNo { get; set; }
        public string Ref1 { get; set; }
        public string ChqNo { get; set; }
        public decimal? CrAmt { get; set; }
        public decimal? DrAmt { get; set; }
        public int? SeqNo { get; set; }
        public int? LogMth { get; set; }
        public decimal? OpeningBalance { get; set; }
        public decimal? ClosingBalance { get; set; }
        public decimal? GLOpeningBalance { get; set; }
        public decimal? GLClosingBalance { get; set; }
        public string AcName { get; set; }
        public string AcName1 { get; set; }
        public string CctName { get; set; }
    }
}