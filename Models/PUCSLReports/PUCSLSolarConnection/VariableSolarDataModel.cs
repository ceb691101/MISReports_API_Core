using System;

namespace MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection
{
    /// <summary>
    /// Model for Variable Solar Data Submission Report (Report 2).
    /// Tracks solar installations by generation capacity ranges.
    /// Data is grouped by tariff category and capacity bands:
    ///   - 0 < capacity <= 20 kW
    ///   - 20 < capacity <= 100 kW
    ///   - 100 < capacity <= 500 kW
    ///   - capacity > 500 kW
    /// </summary>
    public class VariableSolarDataModel
    {
        // Grouping / filter identifiers
        public string Category { get; set; }           // tariff category (D, R, I1, GP1, etc.)
        public string Year { get; set; }               // derived from calc_cycle / bill_cycle
        public string Month { get; set; }              // derived from calc_cycle / bill_cycle

        // ===== 0 < Capacity <= 20 kW =====
        public int NoOfCustomers0To20 { get; set; }
        public decimal KwhUnits0To20 { get; set; }
        public decimal PaidAmount0To20 { get; set; }

        // ===== 20 < Capacity <= 100 kW =====
        public int NoOfCustomers20To100 { get; set; }
        public decimal KwhUnits20To100 { get; set; }
        public decimal PaidAmount20To100 { get; set; }

        // ===== 100 < Capacity <= 500 kW =====
        public int NoOfCustomers100To500 { get; set; }
        public decimal KwhUnits100To500 { get; set; }
        public decimal PaidAmount100To500 { get; set; }

        // ===== Capacity > 500 kW =====
        public int NoOfCustomersAbove500 { get; set; }
        public decimal KwhUnitsAbove500 { get; set; }
        public decimal PaidAmountAbove500 { get; set; }

        // ===== Aggregator Scheme (optional - for future use) =====
        public int NoOfCustomersAggregator { get; set; }
        public decimal KwhUnitsAggregator { get; set; }
        public decimal PaidAmountAggregator { get; set; }

        // Error handling
        public string ErrorMessage { get; set; }
    }
}