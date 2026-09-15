using System;
namespace MISReports_Api.Models.Accounts
{
    public class MaterialReqJobwiseNoMatModel
    {
        public string Category { get; set; }
        public string DocNo { get; set; }
        public string DocPf { get; set; }
        public DateTime? ReqDt { get; set; }
        public string IssueDocPf { get; set; }
        public string IssueDocNo { get; set; }
        public string ReqSource { get; set; }
        public decimal? ReqCost { get; set; }
        public DateTime? AprDt1 { get; set; }
        public DateTime? AprDt2 { get; set; }
        public DateTime? PostDt { get; set; }
        public string TranStatus { get; set; }
        public string BranchName { get; set; }
    }
}