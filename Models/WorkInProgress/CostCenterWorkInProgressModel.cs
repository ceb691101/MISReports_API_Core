using System;

namespace MISReports_Api.Models
{
    public class CostCenterWorkInProgressModel
    {
        public string AssignmentYear { get; set; }
        public string ProjectNo { get; set; }
        public string CategoryCode { get; set; }
        public string Description { get; set; }
        public string FundSource { get; set; }
        public int WipYear { get; set; }
        public int WipMonth { get; set; }
        public string PivNo { get; set; }
        public decimal? GrandTotal { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? SoftCloseDate { get; set; }
        public decimal? EstimatedCost { get; set; }
        public string Status { get; set; }
        public string ResourceType { get; set; }
        public decimal CommittedCost { get; set; }
        public string CctName { get; set; }
    }
}