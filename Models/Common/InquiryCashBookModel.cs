using System;
namespace MISReports_Api.Models.Accounts
{
    public class InquiryCashBookModel
    {
        public string Category { get; set; }
        public string DocNo { get; set; }
        public string DocPf { get; set; }
        public DateTime? DocDt { get; set; }
        public string EntBy { get; set; }
        public string ModiBy { get; set; }
        public string ApprvUid1 { get; set; }
        public string TranStatus { get; set; }
        public string BranchName { get; set; }
    }
}