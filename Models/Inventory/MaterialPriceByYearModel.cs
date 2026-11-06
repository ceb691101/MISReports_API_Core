using System;

namespace MISReports_Api.Models.Inventory
{
    public class MaterialPriceByYearModel
    {
        public string WrhCd { get; set; }
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string FinMth { get; set; }
        public decimal? UnitPrice { get; set; }
        public string CctName { get; set; }
    }
}