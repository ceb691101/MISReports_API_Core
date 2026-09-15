using System;

namespace MISReports_Api.Models.Dashboard
{
    public class OrdinaryCustomers
    {
        public int TotalCount { get; set; }
        public string BillCycle { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class OrdinaryCustomersRequest
    {
        public string BillCycle { get; set; }
    }
}