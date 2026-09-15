using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class SMSRegisteredCustomersModel
    {
        public string LocationName { get; set; }
        public List<MonthlyCount> MonthlyCounts { get; set; } = new List<MonthlyCount>();
    }

    public class MonthlyCount
    {
        public string BillCycle { get; set; }
        public int Count { get; set; }
    }

    public class SMSUsageRequest
    {
        public string FromBillCycle { get; set; }
        public string ToBillCycle { get; set; }
        public string ReportType { get; set; }
        public string TypeCode { get; set; }
    }
}