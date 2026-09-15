using System;

namespace MISReports_Api.Models
{
    public class LedgerCardTotalModel
    {
        public string GlCd { get; set; }
        public string SubAc { get; set; }
        public decimal? OpBal { get; set; }
        public decimal? DrAmt { get; set; }
        public decimal? CrAmt { get; set; }
        public decimal? ClBal { get; set; }
        public string AcName { get; set; }
        public decimal? GLOpeningBalance { get; set; }
        public decimal? GLClosingBalance { get; set; }
        public string CctName { get; set; }
    }
}