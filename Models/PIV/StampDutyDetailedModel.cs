using System;

namespace MISReports_Api.Models.PIV
{
    public class StampDutyDetailedModel
    {
        public string DeptId { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? PivDate { get; set; }
        public string PivNo { get; set; }
        public decimal? Amount { get; set; }
        public int StampDuty { get; set; }          // always 25
        public string CompanyName { get; set; }
    }
}