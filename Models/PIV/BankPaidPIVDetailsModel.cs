using System;

namespace MISReports_Api.Models.PIV
{
    public class BankPaidPIVDetailsModel
    {
        public DateTime? PaidDate { get; set; }
        public string BankCheckNo { get; set; }
        public string PaidAgent { get; set; }
        public string PaidBranch { get; set; }
        public decimal? PaidAmount { get; set; }
        public string DeptId { get; set; }
    }
}