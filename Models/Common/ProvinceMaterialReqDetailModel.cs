using System;
namespace MISReports_Api.Models.Accounts
{
    public class ProvinceMaterialReqDetailModel
    {
        public string Category { get; set; }
        public string DeptId { get; set; }
        public string DocNo { get; set; }
        public string DocPf { get; set; }
        public DateTime? ReqDt { get; set; }
        public string EntBy { get; set; }
        public string ModiBy { get; set; }
        public string AprUid1 { get; set; }
        public string TranStatus { get; set; }
        public string BranchName { get; set; }
    }
}