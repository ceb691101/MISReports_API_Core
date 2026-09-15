namespace MISReports_Api.Models.SolarInformation
{
    public class SolarPVBulkConnectionModel
    {
        public string Division { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }
        public string CustomerName { get; set; }
        public string AccountNumber { get; set; }
        public string CustomerType { get; set; }
        public decimal PanelCapacity { get; set; }
        public int BFUnits { get; set; }
        public int EnergyExported { get; set; }
        public int EnergyImported { get; set; }
        public int CFUnits { get; set; }
        public string SinNumber { get; set; }
        public string Tariff { get; set; }
        public string AgreementDate { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SolarPVBulkConnectionRequest
    {
        public string BillCycle { get; set; }
        public string CalcCycle { get; set; }
        public string CycleType { get; set; } // "A" for bill_cycle, "C" for calc_cycle
        public SolarReportType ReportType { get; set; }
        public string AreaCode { get; set; }
        public string ProvCode { get; set; }
        public string Region { get; set; }
    }

    public class BulkBillCycleModel
    {
        public string BillCycle { get; set; }
        public string CalcCycle { get; set; }
        public string ErrorMessage { get; set; }
    }
}