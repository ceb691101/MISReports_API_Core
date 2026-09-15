using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class FundSummaryModel
    {
        public string DeptId { get; set; }
        public string ProjectNo { get; set; }
        public decimal? TotalLength { get; set; }
        public decimal? PremisesLength { get; set; }
        public decimal? CustomerAmount { get; set; }
        public string ResType { get; set; }
        public decimal? TotCost { get; set; }
        public decimal? CebCost { get; set; }
        public string CctName { get; set; }
        public string AreaName { get; set; }
    }
}