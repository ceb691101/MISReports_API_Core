using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class BulkConnectionDetailsModel
    {
        public DateTime? PrjAssDt { get; set; }
        public string ProjectNo { get; set; }
        public string Provision { get; set; }
        public string DeptId { get; set; }
        public string LineType { get; set; }
        public decimal? MvLine { get; set; }
        public string LineDes { get; set; }
        public decimal? LineCost { get; set; }
        public decimal? Demand { get; set; }
        public decimal? StandardCost { get; set; }
        public decimal? CebCost { get; set; }
        public decimal? StandardRebateCost { get; set; }
        public decimal? ConsumerPayable { get; set; }
        public decimal? ConstructionRebateAmount { get; set; }
        public decimal? WorkEstimateActualCost { get; set; }
    }
}