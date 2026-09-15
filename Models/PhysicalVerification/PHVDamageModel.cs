using System;

namespace MISReports_Api.Models.PhysicalVerification
{
    public class PHVDamageModel
    {
        public DateTime? PhvDate { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string UomCode { get; set; }
        public string GradeCode { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal QtyOnHand { get; set; }
        public string DocumentNo { get; set; }
        public decimal StockBook { get; set; }
        public string Reason { get; set; }
    }
}
