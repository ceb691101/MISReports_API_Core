//19. POS Paid PIV Tabulation Summary Report (AFMHQ)

// PosPaidPivTabulationSummaryAfmhqModel.cs
namespace MISReports_Api.Models
{
    public class PosPaidPivTabulationSummaryAfmhqModel
    {
        public string Company { get; set; }         // e.g. "Division - Head Quarters", "Cost Center - XXXX", "Branch - YYYY"
        public string Account_Code { get; set; }
        public string C8 { get; set; }              // same as account_code (alias c8)
        public decimal Amount { get; set; }
        public string CCT_NAME1 { get; set; }       // Name of the paid department (cost center name)
    }
}