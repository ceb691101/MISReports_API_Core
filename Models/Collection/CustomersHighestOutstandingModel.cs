using System;

namespace MISReports_Api.Models.Collection
{
    public class CustomersHighestOutstandingRequest
    {
        public string Scope { get; set; } // "Province" or "Division"
        public string ProvinceCode { get; set; }
        public string RegionCode { get; set; }
        public int MonthsInArrears { get; set; }
        public decimal OutstandingBalance { get; set; }
    }

    public class CustomersHighestOutstandingModel
    {
        public string Province { get; set; }
        public string AreaName { get; set; }
        public string AccountNumber { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string Telephone { get; set; }
        public string LastCashDate { get; set; }
        public string CurrentReadingDate { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal KwhCharge { get; set; }
        public decimal ArrearsBalance { get; set; }
        public string TariffCode { get; set; }
        public decimal ArrearsMonths { get; set; }
        public decimal Units { get; set; }
    }
}
