using System;

namespace MISReports_Api.Models.SolarInformation
{
    public class SolarReadingSummaryModel
    {
        public string Category { get; set; }
        public string Tariff { get; set; }
        public int NoOfCustomers { get; set; }
        public decimal ExportUnits { get; set; }
        public decimal ImportUnits { get; set; }
        public decimal UnitsBill { get; set; }
        public decimal Payments { get; set; }

        // Additional fields for internal use
        public string ErrorMessage { get; set; }
    }
}