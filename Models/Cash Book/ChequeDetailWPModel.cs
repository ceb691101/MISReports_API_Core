using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models
{
    public class ChequeDetailWPModel
    {
        public DateTime? ChqDt { get; set; }
        public string ChqNo { get; set; }
        public string PymtDocNo { get; set; }
        public string ExpCd { get; set; }
        public decimal? DrAmt { get; set; }
        public decimal? ChqAmt { get; set; }
        public string Payee { get; set; }
        public string Remarks { get; set; }
        public string Address { get; set; }
        public string CctName { get; set; }
    }
}