using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class MaterialFlowModel
    {
        public string DocNo { get; set; }
        public string TrxType { get; set; }
        public string IssRef { get; set; }
        public decimal? AddOrSub { get; set; }
        public DateTime? TrxDate { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public int? Addition { get; set; }
        public string CctName { get; set; }
        public decimal? QtyOnHandP { get; set; }
        public decimal? QIn { get; set; }
        public decimal? QOut { get; set; }
        public decimal? CIn { get; set; }
        public decimal? COut { get; set; }
    }
}