using System.Collections.Generic;

namespace MISReports_Api.Models.Dashboard
{
    public class TopCustomerRecord
    {
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public decimal Kwh { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TopCustomersResponse
    {
        public string BillCycle { get; set; }
        public List<TopCustomerRecord> Records { get; set; }
        public string ErrorMessage { get; set; }
    }
}