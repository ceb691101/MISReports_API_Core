namespace MISReports_Api.Models.SolarInformation
{
    public class RetailSummaryModel
    {
        public string NetType { get; set; }
        public int NoOfAccounts { get; set; }
        public int EnergyExported { get; set; }
        public int EnergyImported { get; set; }
        public int UnitSaleKwh { get; set; }
        public decimal UnitSaleRs { get; set; }
        public decimal KwhPayableBalance { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class RetailSummaryRequest
    {
        public string BillCycle { get; set; }
        public string CalcCycle { get; set; }
        public string CycleType { get; set; } // "A" for bill_cycle, "C" for calc_cycle
    }
}