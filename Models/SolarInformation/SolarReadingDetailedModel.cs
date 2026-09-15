using System;

namespace MISReports_Api.Models.SolarInformation
{
    public class SolarReadingDetailedModel
    {
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string Tariff { get; set; }
        public string MeterNumber { get; set; }
        public string PresentReadingDate { get; set; }
        public string PreviousReadingDate { get; set; }
        public decimal PresentReadingImport { get; set; }
        public decimal PreviousReadingImport { get; set; }
        public decimal UnitsIn { get; set; }
        public decimal PresentReadingExport { get; set; }
        public decimal PreviousReadingExport { get; set; }
        public decimal UnitsOut { get; set; }
        public decimal NetUnits { get; set; }
        public decimal UnitCost { get; set; }
        public decimal PayableAmount { get; set; }
        public string BankCode { get; set; }
        public string BranchCode { get; set; }
        public string BankAccountNumber { get; set; }
        public string AgreementDate { get; set; }

        // Additional fields for internal use
        public string AreaCode { get; set; }
        public string Area { get; set; }
        public string Province { get; set; }
        public string Division { get; set; }
        public string BillCycle { get; set; }
        public string ErrorMessage { get; set; }
    }
}