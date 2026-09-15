using System;

namespace ChqApp.Models
{
    public class CashSheetDateRangePayeeModel
    {
        public string ChqRun { get; set; } = string.Empty;
        public DateTime? ChqDt { get; set; }
        public string Payee { get; set; } = string.Empty;
        public string PymtDocNo { get; set; } = string.Empty;
        public decimal? ChqAmt { get; set; }
        public string ChqNo { get; set; } = string.Empty;
        public string CctName { get; set; } = string.Empty;
    }
}