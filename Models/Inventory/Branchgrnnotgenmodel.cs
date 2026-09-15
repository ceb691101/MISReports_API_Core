using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class BranchGrnNotGenModel
    {
        public string DocNo { get; set; }
        public DateTime? TrxDt { get; set; }
        public string DesDeptId { get; set; }
        public string WrhCd { get; set; }
        public string Ref1 { get; set; }
        public decimal? TrxnVal { get; set; }
        public string BranchName { get; set; }
    }
}