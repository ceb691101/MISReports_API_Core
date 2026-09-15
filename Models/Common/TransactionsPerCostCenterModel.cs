using System;
namespace MISReports_Api.Models.Accounts
{
    public class TransactionsPerCostCenterModel
    {
        public string Category { get; set; }
        public DateTime? EnteredDate { get; set; }
        public int Count { get; set; }
        public string BranchName { get; set; }
    }
}