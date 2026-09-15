namespace MISReports_Api.Models.SolarInformation
{
    public class RetailDetailedModel
    {
        public string Division { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }
        public string CustomerName { get; set; }
        public string AccountNumber { get; set; }
        public decimal PanelCapacity { get; set; }
        public int EnergyExported { get; set; }
        public int EnergyImported { get; set; }
        public string Tariff { get; set; }
        public int BFUnits { get; set; }
        public int UnitsInBill { get; set; }
        public int Period { get; set; }
        public decimal KwhCharge { get; set; }
        public decimal FixedCharge { get; set; }
        public decimal FuelCharge { get; set; }
        public int CFUnits { get; set; }
        public decimal Rate { get; set; }
        public int UnitSale { get; set; }
        public decimal KwhSales { get; set; }
        public string BankCode { get; set; }
        public string BranchCode { get; set; }
        public string BankAccountNumber { get; set; }
        public string AgreementDate { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class RetailDetailedRequest
    {
        public string BillCycle { get; set; }
        public string CalcCycle { get; set; }
        public string CycleType { get; set; } // "A" for bill_cycle, "C" for calc_cycle
        public string NetType { get; set; } // Net type filter (1, 2, 3, 4, 5)
        public SolarReportType ReportType { get; set; }
        public string AreaCode { get; set; }
        public string ProvCode { get; set; }
        public string Region { get; set; }
    }
}