using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class ChequeDetailsExpRegionModel
    {
        public string DeptId { get; set; }
        public DateTime? ChqDt { get; set; }
        public string ChqNo { get; set; }
        public string PymtDocNo { get; set; }
        public string ExpCd { get; set; }
        public decimal? DrAmt { get; set; }
        public decimal? ChqAmt { get; set; }
        public string Payee { get; set; }
        public string Remarks { get; set; }
        public string ChqRun { get; set; }
        public DateTime? RunDt { get; set; }
        public string CctName { get; set; }
    }
}