namespace MISReports_Api.Models.General
{
    public class TotalCustomerBaseRequest
    {
        public string AreaType { get; set; } = "Entire CEB";
        public string LocationCode { get; set; }
        public string BillCycle { get; set; }
    }

    public class TotalCustomerBaseRow
    {
        public string ProvinceName { get; set; }
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
        public long AgTotal { get; set; }
        public long FTotal { get; set; }
    }

    public class TotalCustomerBaseBillCycle
    {
        public string Code { get; set; }
        public string Label { get; set; }
    }

    public class TotalCustomerBaseResponse
    {
        public int CurrentCycle { get; set; }
        public string CurrentCycleLabel { get; set; }
        public int PreviousCycle { get; set; }
        public string PreviousCycleLabel { get; set; }
        public System.Collections.Generic.List<TotalCustomerBaseRow> Rows { get; set; }
    }
}
