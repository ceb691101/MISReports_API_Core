using System;
namespace MISReports_Api.Models.Accounts
{
    public class MaterialReqJobwiseModel
    {
        public string DocNo { get; set; }
        public string DocPf { get; set; }
        public DateTime? ReqDt { get; set; }
        public string ResCd { get; set; }
        public decimal? ReqUnits { get; set; }
        public decimal? ReqCost { get; set; }
        public string IssueDocPf { get; set; }
        public string IssueDocNo { get; set; }
        public string ReqSource { get; set; }
        public decimal? IssuedQty { get; set; }
        public decimal? IssuedVal { get; set; }
        public string TranStatus { get; set; }
        public decimal? EstQty { get; set; }
        public decimal? ComQty { get; set; }
    }
}