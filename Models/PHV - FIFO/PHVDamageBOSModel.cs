using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class PHVDamageBOSModel
    {
        public string DocNo { get; set; }
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string GradeCd { get; set; }
        public decimal? DamageCount { get; set; }
        public string BatchId { get; set; }
        public decimal? QtyOnHand { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? StockBook { get; set; }
        public string Reason { get; set; }
        public string CctName { get; set; }
    }
}