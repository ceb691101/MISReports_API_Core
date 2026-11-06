using System.Collections.Generic;

namespace MISReports_Api.Models.PUCSLReports
{
    public class SolarJournalsRetailRequest
    {
        public string AreaType { get; set; }
        public string LocationCode { get; set; }
        public string BillCycle { get; set; }
    }

    public class SolarJournalsRetailRow
    {
        public string AreaName { get; set; }
        public string AccountNumber { get; set; }
        public string TransactionCode { get; set; }
        public string TransactionDescription { get; set; }
        public decimal? TransactionAmount { get; set; }
        public string CreditDebitCode { get; set; }
        public string NetType
        {
            get
            {
                switch (TransactionCode)
                {
                    case "JM": return "Net Accounting";
                    case "JR": return "Net Plus";
                    case "JT": return "Net Plus Plus";
                    default: return "";
                }
            }
        }
        public decimal? CreditAmount => CreditDebitCode == "C" ? TransactionAmount : null;
        public decimal? DebitAmount => CreditDebitCode == "D" ? TransactionAmount : null;
    }

    public class SolarJournalsRetailResponse
    {
        public List<SolarJournalsRetailRow> Rows { get; set; } = new List<SolarJournalsRetailRow>();
        public decimal TotalAmount { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
    }
}
