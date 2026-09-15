using System;

namespace MISReports_Api.Models
{
    public class JobcardModel
    {
        public string ProjectNo { get; set; }
        public decimal CommitedCost { get; set; }
        public string Description { get; set; }
        public decimal EstimatedCost { get; set; }
        public string FundSource { get; set; }
        public string EstimateNo { get; set; }
        public DateTime? ProjectAssignedDate { get; set; }
        public string Status { get; set; }
        public int LogYear { get; set; }
        public int LogMonth { get; set; }
        public string DocumentProfile { get; set; }
        public string DocumentNo { get; set; }
        public DateTime? AccDate { get; set; }
        public int SequenceNo { get; set; }
        public decimal TrxAmt { get; set; }
        public string ResType { get; set; }
    }
}