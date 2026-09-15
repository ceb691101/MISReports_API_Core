using System;

namespace MISReports_Api.Models.PIV
{
    public class RegionalPIVStampDutyModel
    {
        public DateTime? Paid_Date { get; set; }
        public string Pay_type { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public decimal Stamp_Duty { get; set; }
        public string Comp_nm { get; set; }
    }
}