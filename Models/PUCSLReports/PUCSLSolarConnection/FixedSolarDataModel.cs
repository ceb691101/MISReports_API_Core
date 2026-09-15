using System;

namespace MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection
{
    public class FixedSolarDataModel
    {
        // Grouping / filter identifiers
        public string Category { get; set; }           // tariff_cat from tariff_category
        public string Year { get; set; }               // derived from calc_cycle / bill_cycle
        public string Month { get; set; }              // derived from calc_cycle / bill_cycle

        // --- Ordinary counts ---
        public int OrdinaryNoOfCustomers { get; set; }

        // --- Bulk counts ---
        public int BulkNoOfCustomers { get; set; }

        // Combined No of Customers  (Ordinary + Bulk)
        public int NoOfCustomers { get; set; }

        // --- kWh Purchased (Ordinary) at each rate ---
        public decimal OrdinaryKwhAt1550 { get; set; }
        public decimal OrdinaryKwhAt22 { get; set; }
        public decimal OrdinaryKwhAt3450 { get; set; }
        public decimal OrdinaryKwhAt37 { get; set; }
        public decimal OrdinaryKwhAt2318 { get; set; }
        public decimal OrdinaryKwhAt2706 { get; set; }
        public decimal OrdinaryKwhOthers { get; set; }

        // --- kWh Purchased (Bulk) at each rate ---
        public decimal BulkKwhAt1550 { get; set; }
        public decimal BulkKwhAt22 { get; set; }
        public decimal BulkKwhAt3450 { get; set; }
        public decimal BulkKwhAt37 { get; set; }
        public decimal BulkKwhAt2318 { get; set; }
        public decimal BulkKwhAt2706 { get; set; }

        // --- Combined kWh Purchased (Ordinary + Bulk) returned to frontend ---
        public decimal KwhAt1550 { get; set; }
        public decimal KwhAt22 { get; set; }
        public decimal KwhAt3450 { get; set; }
        public decimal KwhAt37 { get; set; }
        public decimal KwhAt2318 { get; set; }
        public decimal KwhAt2706 { get; set; }
        public decimal KwhOthers { get; set; }         // Ordinary-only "Others" bucket

        // --- Paid Amount (sum of all kwh_sales: ordinary + bulk) ---
        public decimal PaidAmount { get; set; }

        // Internal / error
        public string ErrorMessage { get; set; }
    }

    
}