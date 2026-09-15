using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class CurrentAcctBalCCModel
    {
        public string SubAc { get; set; }
        public string AcNm { get; set; }
        public decimal? ClBal { get; set; }
        public string CctName { get; set; }
    }
}