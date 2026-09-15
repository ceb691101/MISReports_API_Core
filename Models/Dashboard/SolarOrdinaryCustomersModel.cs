namespace MISReports_Api.Models.Dashboard
{
    using System.Collections.Generic;

    public class SolarOrdinaryCustomersSummary
    {
        public string BillCycle { get; set; }
        public int TotalCustomers { get; set; }
        public int NetMeteringCustomers { get; set; }
        public int NetAccountingCustomers { get; set; }
        public int NetPlusCustomers { get; set; }
        public int NetPlusPlusCustomers { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarOrdinaryCustomersCount
    {
        public string BillCycle { get; set; }
        public int CustomersCount { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarOrdinaryGenerationCapacityPoint
    {
        public string BillCycle { get; set; }
        public string NetType { get; set; }
        public int AccountsCount { get; set; }
        public decimal CapacityKw { get; set; }
    }

    public class SolarOrdinaryGenerationCapacityGraph
    {
        public string MaxBillCycle { get; set; }
        public string SelectedBillCycle { get; set; }
        public List<string> AvailableBillCycles { get; set; }
        public List<SolarOrdinaryGenerationCapacityPoint> Records { get; set; }
        public string ErrorMessage { get; set; }
    }
}