using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection
{
    /// <summary>
    /// Model for Raw Data for Solar Report (Report 4).
    /// Returns Net Metering energy flow data with separate Ordinary and Bulk sections with totals.
    /// Structure matches the PUCSL report format exactly.
    /// </summary>
    public class RawDataForSolarResponse
    {
        public List<RawSolarData> Ordinary { get; set; }
        public RawSolarData OrdinaryTotal { get; set; }
        public List<RawSolarData> Bulk { get; set; }
        public RawSolarData BulkTotal { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Raw solar energy data matching the screenshot structure.
    /// Both Ordinary and Bulk use the same model structure.
    /// Columns: Category, Year, Month, Day, Import (Peak/Off Peak/Day), Export (Peak/Off Peak/Day), 
    /// Brought Forward kWh, Carry Forward kWh
    /// </summary>
    public class RawSolarData
    {
        public string Category { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }

        // Import section
        public decimal ImportDay { get; set; }
        public decimal ImportPeak { get; set; }
        public decimal ImportOffPeak { get; set; }

        // Export section
        public decimal ExportDay { get; set; }
        public decimal ExportPeak { get; set; }
        public decimal ExportOffPeak { get; set; }

        // Brought Forward kWh (Only in Net Metering)
        public decimal BroughtForwardKwh { get; set; }

        // Carry Forward kWh (Only in Net Metering)
        public decimal CarryForwardKwh { get; set; }
    }
}