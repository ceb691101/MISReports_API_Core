using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.SolarInformation
{
    // Each row returned from the database
    public class SolarJournalsBulkItem
    {
        public string AreaName { get; set; }           // a.area_name
        public string AccountNumber { get; set; }      // t.acc_nbr
        public string NetType { get; set; }            // c.net_type (2, 3, 4)
        public string SchemeName { get; set; }         // Net Accounting / Net Plus / Net Plus Plus
        public string JournalDescription { get; set; } // j.jnl_desc
        public decimal TxnAmount { get; set; }         // t.txn_amt
        public string AmountStatus { get; set; }       // t.amt_stat
    }

    // Request sent from the frontend
    public class SolarJournalsBulkRequest
    {
        public string BillCycle { get; set; }          // 456, 457 etc.
        public string ReportCategory { get; set; }     // "Area", "Province", "Division", "EntireCEB"
        public string TypeCode { get; set; }           // Area code / Prov code / Region code
    }

    // Frontend එකට යවන Response එක
    public class SolarJournalsBulkResponse
    {
        public string BillCycle { get; set; }
        public string ReportCategory { get; set; }
        public string TypeCode { get; set; }
        public List<SolarJournalsBulkItem> Rows { get; set; } = new List<SolarJournalsBulkItem>();
        public decimal TotalAmount { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
    }
}