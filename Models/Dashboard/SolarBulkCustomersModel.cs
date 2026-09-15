namespace MISReports_Api.Models.Dashboard
{
    using System.Collections.Generic;

    public class SolarBulkCustomersCount
    {
        public int CustomersCount { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarBulkCustomersSummary
    {
        public int TotalCustomers { get; set; }
        public int NetType1Customers { get; set; }
        public int NetType2Customers { get; set; }
        public int NetType3Customers { get; set; }
        public int NetType4Customers { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarBulkGenerationCapacityPoint
    {
        public string BillCycle { get; set; }
        public string NetType { get; set; }
        public int AccountsCount { get; set; }
        public decimal CapacityKw { get; set; }
    }

    public class SolarBulkGenerationCapacityGraph
    {
        public string MaxBillCycle { get; set; }
        public string SelectedBillCycle { get; set; }
        public List<string> AvailableBillCycles { get; set; }
        public List<SolarBulkGenerationCapacityPoint> Records { get; set; }
        public string ErrorMessage { get; set; }
    }
}