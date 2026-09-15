using System;

namespace MISReports_Api.Models.SolarInformation
{
    public class SolarReadingUsageBulkModel
    {
        // Common fields for all report types
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string Tariff { get; set; }
        public string NetType { get; set; }
        public string MeterNumber { get; set; }
        public string PresentReadingDate { get; set; }
        public string PreviousReadingDate { get; set; }

        // Import readings (mtr_seq=1)
        public string PresentReadingImport { get; set; }
        public string PreviousReadingImport { get; set; }
        public string UnitsIn { get; set; }

        // Export readings (mtr_seq=2)
        public string PresentReadingExport { get; set; }
        public string PreviousReadingExport { get; set; }
        public string UnitsOut { get; set; }

        // Calculated fields
        public decimal NetUnits { get; set; }
        public decimal UnitCost { get; set; }
        public string AgreementDate { get; set; }

        // Location fields (for Province, Region, and Entire CEB reports)
        public string Division { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }

        // Internal fields
        public string AreaCode { get; set; }
        public string BillCycle { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request model for Solar Reading & Usage Bulk reports
    /// </summary>
    public class BulkUsageRequest
    {
        public string AddedBillCycle { get; set; }  // added_blcy parameter
        public string BillCycle { get; set; }        // bill_cycle parameter for netmtcons
        public string NetType { get; set; }          // Net type (1-5)
        public SolarReportType ReportType { get; set; }

        // Location filters based on report type
        public string AreaCode { get; set; }    // For Area reports
        public string ProvCode { get; set; }    // For Province reports
        public string Region { get; set; }      // For Region reports
    }
}