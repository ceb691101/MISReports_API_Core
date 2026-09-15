using System;
namespace MISReports_Api.Models.Accounts
{
    public class ChequeCancellationDivisionModel
    {
        public string DeptId { get; set; }
        public string DocNo { get; set; }
        public DateTime? ChqDt { get; set; }
        public string ChqNo { get; set; }
        public decimal? ChqAmt { get; set; }
        public string ChqRun { get; set; }
        public DateTime? RunDt { get; set; }
        public string BranchName { get; set; }
    }
}