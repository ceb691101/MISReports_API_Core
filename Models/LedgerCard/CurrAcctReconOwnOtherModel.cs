using System;

namespace MISReports_Api.Models
{
    public class CurrAcctReconOwnOtherModel
    {
        public string CatCode { get; set; }
        public string SubAc { get; set; }
        public decimal? ClBal { get; set; }
        public string AcName { get; set; }
        public string CctName { get; set; }
    }
}
