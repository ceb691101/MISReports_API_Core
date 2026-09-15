using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.Collection
{
    // ── Report type enum ───────────────────────────────────────────────────────
    public enum SalesCollectionReportType
    {
        Province,
        Region,
        EntireCEB
    }

    // ── Request model ──────────────────────────────────────────────────────────
    public class SalesAndCollectionRequest
    {
        public string BillCycle { get; set; }
        public SalesCollectionReportType ReportType { get; set; }
        public string ProvinceCode { get; set; }   // used when ReportType == Province
        public string RegionCode { get; set; }     // used when ReportType == Region
    }

    // ── Main report row model ──────────────────────────────────────────────────
    public class SalesAndCollectionModel
    {
        // Grouping identifiers
        public string ProvinceCode { get; set; }
        public string AreaCode { get; set; }
        public string AreaName { get; set; }

        // --- Ordinary supply (from ordinary DB receive_position) ---
        // ord_sup = mon_chg + debits - credits
        public decimal RawOrdinarySupply { get; set; }

        // --- Bulk supply (from bulk DB receive_position) ---
        // bulk_sup = mon_chg + debits - credits
        public decimal RawBulkSupply { get; set; }

        // --- Ordinary collection (from ordinary DB receive_position) ---
        // ord_collect = payments
        public decimal RawOrdinaryCollection { get; set; }

        // --- Bulk collection (from bulk DB receive_position) ---
        // bulk_collect = payments
        public decimal RawBulkCollection { get; set; }

        // --- Computed fields ---
        public decimal RawTotalNetSales => RawOrdinarySupply + RawBulkSupply;
        public decimal RawTotalCollections => RawOrdinaryCollection + RawBulkCollection;
        public decimal RawCollectionPercentage =>
            RawTotalNetSales == 0 ? 0
            : Math.Round((RawTotalCollections / RawTotalNetSales) * 100, 2);

        // --- Formatted display strings ---
        public string OrdinarySupply { get; set; }       // Ordinary Supply (Net)
        public string BulkSupply { get; set; }           // Heavy Supply (Net)
        public string TotalNetSales { get; set; }        // Total Net Sales (without Street Lights)
        public string OrdinaryCollection { get; set; }   // Ordinary Supply (Collections)
        public string BulkCollection { get; set; }       // Bulk Supply (Collections)
        public string TotalCollections { get; set; }     // Collections on Sales (Without Street Lights)
        public string CollectionPercentage { get; set; } // % of Collections on Sales

        public string ErrorMessage { get; set; }
    }
}