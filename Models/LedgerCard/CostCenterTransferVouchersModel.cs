using System;

namespace MISReports_Api.Models
{
    public class CostCenterTransferVouchersModel
    {
        public string DocPf { get; set; }
        public string TrfType { get; set; }
        public string SubAc { get; set; }
        public string Remarks { get; set; }
        public DateTime? AcctDt { get; set; }
        public string DocNo { get; set; }
        public string Ref1 { get; set; }
        public string ChqNo { get; set; }
        public decimal? CrAmt { get; set; }
        public decimal? DrAmt { get; set; }
        public int? LogMth { get; set; }
        public string DesgDept { get; set; }
        public string CctName { get; set; }
    }

    public class DocProfileModel
    {
        public string doc_pf { get; set; }
        public string doc_desc { get; set; }
    }
}
