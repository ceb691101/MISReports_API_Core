using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class ChequeDetailsExpModel
    {
        public DateTime? ChqDt { get; set; }
        public string ChqNo { get; set; }
        public string PymtDocNo { get; set; }
        public string ExpCd { get; set; }
        public decimal? DrAmt { get; set; }
        public decimal? ChqAmt { get; set; }
        public string Payee { get; set; }
        public string Remarks { get; set; }
        public string Ref1 { get; set; }
        public string Ref2 { get; set; }
        public string Ref3 { get; set; }
        public string Ref4 { get; set; }
        public string CctName { get; set; }
    }
}