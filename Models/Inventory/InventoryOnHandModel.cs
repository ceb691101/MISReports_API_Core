using System;

namespace MISReports_Api.Models
{
    public class InventoryOnHandModel
    {
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string GrdCd { get; set; }
        public string MajUom { get; set; }
        public decimal Alocated { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Value { get; set; }
        public string CctName { get; set; }
    }
}