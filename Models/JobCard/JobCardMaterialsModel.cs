using System;

namespace MISReports_Api.Models
{
    public class JobCardMaterialsModel
    {
        public string TrxType { get; set; }
        public string DocNo { get; set; }
        public DateTime? TrxDt { get; set; }
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string GradeCd { get; set; }
        public decimal TrxQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TrxVal { get; set; }  // Positive for issue/receipt, negative for cancel/return
    }
}