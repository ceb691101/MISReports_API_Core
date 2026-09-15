using System.Collections.Generic;

namespace MISReports_Api.Models.PUCSLReports
{
    public class BulkPUCSLSolarCustomersModel
    {
        public string Region { get; set; }
        public string BillCycle { get; set; }
        public string Period { get; set; }        // Human-readable month-year, e.g. "Jan 24"
        public string NetType { get; set; }        // Net Metering / Net Accounting / Net Plus / Net Plus Plus
        public string AreaCode { get; set; }
        public int NoOfAccounts { get; set; }
        public int Sale { get; set; }
        public int Export { get; set; }
        public int Import { get; set; }
        public decimal KwhSales { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request model for the Bulk PUCSL Solar Customers report.
    /// Filters netmtcons/areas by region and a bill cycle range (from/to).
    /// </summary>
    public class BulkPUCSLSolarCustomersRequest
    {
        public string Region { get; set; }         // e.g. "R1", "R2", "R3", "R4"
        public string FromBillCycle { get; set; }   // e.g. "437"
        public string ToBillCycle { get; set; }     // e.g. "448"
    }
}