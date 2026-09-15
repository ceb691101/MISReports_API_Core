// Models/IssueReceiptWPModel.cs
using System;

namespace MISReports_Api.Models
{
    public class IssueReceiptWPModel
    {
        public string DocPf { get; set; }
        public string WrhCd { get; set; }
        public string DocNo { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public DateTime? TrxDt { get; set; }
        public decimal? Total { get; set; }
        public string DesDeptId { get; set; }
        public string CctName { get; set; }
    }
}