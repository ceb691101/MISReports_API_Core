using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class PriceVaModel
    {
        public string MatCd { get; set; }
        public string GradeCd { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? NewPrice { get; set; }
        public decimal? INetChange { get; set; }
        public decimal? DNetChange { get; set; }
        public decimal? QtyOnHand { get; set; }
        public decimal? Var { get; set; }
        public string CctName { get; set; }
    }
}