namespace MISReports_Api.Models.PUCSLReports
{
    public class OrdinaryPUCSLSolarCustomersModel
    {
        public string Region { get; set; }
        public string CalcCycle { get; set; }
        public string Period { get; set; }     // Human-readable month-year, e.g. "Jan 24"
        public string AreaCode { get; set; }
        public string NetType { get; set; }     // Net Metering / Net Accounting / Net Plus / Net Plus Plus
        public int NoOfAccounts { get; set; }
        public int Import { get; set; }
        public int Export { get; set; }
        public int Net { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request model for the Ordinary PUCSL Solar Customers report.
    /// Filters netmtcons/areas (ordinary DB) by region and a calc_cycle range (from/to),
    /// mirroring the bulk version's request shape.
    /// </summary>
    public class OrdinaryPUCSLSolarCustomersRequest
    {
        public string Region { get; set; }          // e.g. "R1", "R2", "R3", "R4"
        public string FromBillCycle { get; set; }    // maps to calc_cycle range start, e.g. "437"
        public string ToBillCycle { get; set; }      // maps to calc_cycle range end, e.g. "448"
    }
}