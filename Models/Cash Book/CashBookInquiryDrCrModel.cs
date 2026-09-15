using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class CashbookInquiryDrCrModel
    {
        public string DocNo { get; set; }
        public DateTime? DocDt { get; set; }
        public string ExpCd { get; set; }
        public string SubAc { get; set; }
        public decimal? DrAmt { get; set; }
        public decimal? CrAmt { get; set; }
        public string NonTaxabl { get; set; }
        public string TranStatus { get; set; }
        public string Payee { get; set; }
        public string Remarks { get; set; }
        public string CctName { get; set; }
    }
}