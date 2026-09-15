using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class QuantityMatFIFOModel
    {
        public string WrhCd { get; set; }
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string GrdCd { get; set; }
        public string MajUom { get; set; }
        public decimal? QtyOnHand { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Value { get; set; }
        public string CctName { get; set; }
    }
}