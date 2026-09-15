using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Dashboard
{
    // Define Dashboard-specific enum for report types
    public enum DashboardReportType
    {
        Area,
        Province,
        Region,
        EntireCEB
    }

    public class BulkCustomerModel
    {
        // Customer basic information
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string Tariff { get; set; }
        public string CustomerStatus { get; set; }
        public string CustomerStatusDescription { get; set; }

        // Location information
        public string AreaCode { get; set; }
        public string Area { get; set; }
        public string Province { get; set; }
        public string Division { get; set; }

        // Contact information
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string MobileNumber { get; set; }

        // Dates
        public string ConnectionDate { get; set; }
        public string LastBillDate { get; set; }
        public string LastPaymentDate { get; set; }

        // Status flags
        public string IsActive { get; set; }
        public string CustomerType { get; set; }

        // Error handling
        public string ErrorMessage { get; set; }
    }

    public class BulkCustomerSummaryModel
    {
        // Summary statistics
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }

        // Location breakdowns (optional)
        public string AreaCode { get; set; }
        public string Area { get; set; }
        public string Province { get; set; }

        // Error handling
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Request model for Bulk Customer reports
    /// </summary>
    public class BulkCustomerRequest
    {
        public string CustomerStatus { get; set; } = "0";  // Default to active customers
        public DashboardReportType ReportType { get; set; }

        // Location filters based on report type
        public string AreaCode { get; set; }    // For Area reports
        public string ProvCode { get; set; }    // For Province reports
        public string Region { get; set; }      // For Region reports

        // Optional filters
        public string Tariff { get; set; }
        public string CustomerType { get; set; }

        // Pagination (optional)
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public bool IsPaginationEnabled { get; set; } = false;
    }
}