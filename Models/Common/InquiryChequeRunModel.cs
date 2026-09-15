using System;
namespace MISReports_Api.Models.Accounts
{
    public class InquiryChequeRunModel
    {
        public string Category { get; set; }
        public string ChqRun { get; set; }
        public string DocPf { get; set; }
        public DateTime? RunDt { get; set; }
        public string RunBy { get; set; }
        public string ModiBy { get; set; }
        public string ApprvUid1 { get; set; }
        public string TranStatus { get; set; }
        public string BranchName { get; set; }
    }
}