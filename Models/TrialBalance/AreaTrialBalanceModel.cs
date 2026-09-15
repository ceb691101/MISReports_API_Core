namespace MISReports_Api.Models
{
    public class AreaTrialBalanceModel
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string TitleFlag { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal ClosingBalance { get; set; }
        public string CompanyName { get; set; }
    }
}
