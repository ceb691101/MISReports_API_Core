using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.General
{
    /// <summary>
    /// Represents one row from the Active Customers (Ordinary) report.
    /// Each row = one (location grouping | billing cycle | tariff class) combination
    /// with the consumer count for that combination.
    /// </summary>
    public class ActiveCustomersOrdinaryModel
    {
        // ── Location fields (populated depending on report type) ─────────────────
        public string Area { get; set; }   // area_name  – Area reports
        public string Province { get; set; }   // prov_name  – Area & Province reports
        public string Division { get; set; }   // region     – Province & Region reports

        // ── Cycle & tariff ───────────────────────────────────────────────────────
        public string BillCycle { get; set; }  // calc_cycle value
        public string TariffClass { get; set; }  // e.g. "D1", "H1", "R1", "AGRI", …

        // ── Count ────────────────────────────────────────────────────────────────
        public long Count { get; set; }  // sum(cnt)

        // ── Internal ─────────────────────────────────────────────────────────────
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents one row from the Active Customers (Bulk) report.
    /// Each row = one (location grouping | bill cycle | tariff) combination.
    /// TM1 is always excluded.
    /// </summary>
    public class ActiveCustomersBulkModel
    {
        // ── Location fields (populated depending on report type) ─────────────────
        public string Province { get; set; }   // prov_name  – Area & Province reports
        public string Area { get; set; }   // area_name  – Area reports
        public string AreaCode { get; set; }   // area_cd    – Area reports
        public string Division { get; set; }   // region     – Province & Region reports

        // ── Cycle & tariff ───────────────────────────────────────────────────────
        public string BillCycle { get; set; }  // bill_cycle value
        public string Tariff { get; set; }  // e.g. "DM1", "GP1", "GP2", … "RL1"

        // ── Count ────────────────────────────────────────────────────────────────
        public long Count { get; set; }  // sum(no_acc)

        // ── Internal ─────────────────────────────────────────────────────────────
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request parameters for Active Customers reports (Ordinary and Bulk).
    /// No location filter – every report type returns ALL entries at that level.
    /// </summary>
    public class ActiveCustomersRequest
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
        public ActiveCustomersReportType ReportType { get; set; }
    }

    public enum ActiveCustomersReportType
    {
        Area,
        Province,
        Region,
        EntireCEB
    }
}