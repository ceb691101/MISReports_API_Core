using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class MaterialRequisitionWithIssueDetailsModel
    {
        public string Category { get; set; }
        public string DocNo { get; set; }
        public DateTime? TrxDt { get; set; }
        public string MatCd { get; set; }
        public decimal? ReqUnits { get; set; }
        public string IssueDocNo { get; set; }
        public string ReqSource { get; set; }
        public string Ref1 { get; set; }
        public string AprUid1 { get; set; }
        public string AprUid2 { get; set; }
        public DateTime? AprDt1 { get; set; }
        public DateTime? AprDt2 { get; set; }
        public DateTime? PostDt { get; set; }
        public decimal? IssuedReturnQty { get; set; }
        public string TranStatus { get; set; }
        public decimal? EstQty { get; set; }
        public decimal? ComQty { get; set; }
        public string CctName { get; set; }
    }
}