using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.General
{
    public class SecurityDepositInterestRequest
    {
        public string ReportType { get; set; } = "Bulk"; // "Bulk" or "Ordinary"
        public int BillCycle { get; set; }
        public string Category { get; set; } = "Area"; // "Area", "Province", "Division", "Entire CEB"
        public string CategoryValue { get; set; } // area_code, prov_code, region
    }

    public class SecurityDepositInterestItem
    {
        public string TariffName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SecurityDepositInterestResponse
    {
        public string ReportType { get; set; }
        public int BillCycle { get; set; }
        public string BillCycleLabel { get; set; }
        public string Category { get; set; }
        public string CategoryValue { get; set; }
        public decimal? InterestRate { get; set; }
        public int InterestYear { get; set; }
        public decimal GrandTotal { get; set; }
        public List<SecurityDepositInterestItem> Rows { get; set; } = new List<SecurityDepositInterestItem>();
    }

    public class SecurityDepositBillCycleOption
    {
        public int BillCycle { get; set; }
        public string Display { get; set; }
        public string Code { get; set; }
    }
}

