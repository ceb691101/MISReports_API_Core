using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class PriceVaWHModel
    {
        public string WrhCd { get; set; }
        public string MatCd { get; set; }
        public string GradeCd { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? NewPrice { get; set; }
        public decimal? NetChange { get; set; }
        public decimal? QtyOnHand { get; set; }
        public decimal? Var { get; set; }
        public string CctName { get; set; }
    }
}