using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class CCWiseIssueModel
    {
        public string Type { get; set; }
        public int? YrInd { get; set; }
        public int? MthInd { get; set; }
        public string TrxType { get; set; }
        public DateTime? TrxDt { get; set; }
        public string DocPf { get; set; }
        public string DocNo { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public decimal? Total { get; set; }
        public string Remarks { get; set; }
        public string IsRef { get; set; }
        public string CctName { get; set; }
    }
}