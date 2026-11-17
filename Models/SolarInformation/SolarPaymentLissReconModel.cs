using System.Collections.Generic;

namespace MISReports_Api.Models.SolarInformation
{
    public class SolarPaymentLissReconItem
    {
        public string NetType { get; set; }
        public string SchemeName { get; set; }

        // The exact 4 columns requested
        public decimal Ordinary { get; set; } // Ordinary Financial Report Total
        public decimal buLK { get; set; }     // Bulk Financial Report Total
        public decimal oLiss { get; set; }    // Ordinary LISS Total (Fixed + Other + Variable)
        public decimal Bliss { get; set; }    // Bulk LISS Total (Fixed + Other + Variable)
    }

    public class SolarPaymentLissReconResponse
    {
        public List<SolarPaymentLissReconItem> Rows { get; set; } = new List<SolarPaymentLissReconItem>();
        public SolarPaymentLissReconItem GrandTotal { get; set; }
        public string BillCycle { get; set; }
        public string ReportCategory { get; set; }
        public string TypeCode { get; set; }
    }

    public class SolarPaymentLissReconRequest
    {
        public string BillCycle { get; set; }
        public string ReportCategory { get; set; }
        public string TypeCode { get; set; }
    }
}
