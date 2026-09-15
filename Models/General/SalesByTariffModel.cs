namespace MISReports_Api.Models.General
{
    /// <summary>
    /// Represents one row from the Sales by Tariff (Ordinary) report.
    /// Each row = one (location grouping | billing cycle | tariff class) combination
    /// with the total kWh sales (cons_kwh) for that combination.
    /// </summary>
    public class SalesByTariffOrdinaryModel
    {
        // ── Location fields (populated depending on report type) ─────────────────
        public string Area { get; set; }   // area_name  – Area reports
        public string Province { get; set; }   // prov_name  – Area & Province reports
        public string Division { get; set; }   // region     – Province & Region reports

        // ── Cycle & tariff ───────────────────────────────────────────────────────
        public string BillCycle { get; set; }  // calc_cycle value
        public string TariffClass { get; set; }  // e.g. "D1", "H1", "R1", "AGRI", …

        // ── Aggregated kWh ───────────────────────────────────────────────────────
        public decimal KwhSales { get; set; }  // sum(cons_kwh)

        // ── Internal ─────────────────────────────────────────────────────────────
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents one row from the Sales by Tariff (Bulk) report.
    /// Each row = one (location grouping | bill cycle | tariff) combination.
    /// TM1 is always excluded.
    /// </summary>
    public class SalesByTariffBulkModel
    {
        // ── Location fields (populated depending on report type) ─────────────────
        public string Province { get; set; }   // prov_name  – Area & Province reports
        public string Area { get; set; }   // area_name  – Area reports
        public string AreaCode { get; set; }   // area_cd    – Area reports
        public string Division { get; set; }   // region     – Province & Region reports

        // ── Cycle & tariff ───────────────────────────────────────────────────────
        public string BillCycle { get; set; }  // bill_cycle value
        public string Tariff { get; set; }  // e.g. "DM1", "GP1", … "DM2"

        // ── Aggregated kWh ───────────────────────────────────────────────────────
        public decimal KwhSales { get; set; }  // sum(kwh_units)

        // ── Internal ─────────────────────────────────────────────────────────────
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request parameters for Sales by Tariff reports (Ordinary and Bulk).
    /// No location filter – every report type returns ALL entries at that level.
    /// </summary>
    public class SalesByTariffRequest
    {
        /// <summary>Start of billing-cycle range (calc_cycle / bill_cycle >= ?).</summary>
        public string FromCycle { get; set; }

        /// <summary>End of billing-cycle range (calc_cycle / bill_cycle &lt;= ?).</summary>
        public string ToCycle { get; set; }

        /// <summary>
        /// Granularity of the report:
        ///   Area      → all areas grouped by area_code
        ///   Province  → all provinces grouped by prov_code
        ///   Region    → all regions grouped by a.Region
        ///   EntireCEB → single aggregate row for the whole CEB
        /// </summary>
        public SalesByTariffReportType ReportType { get; set; }
    }

    public enum SalesByTariffReportType
    {
        Area,
        Province,
        Region,
        EntireCEB
    }
}