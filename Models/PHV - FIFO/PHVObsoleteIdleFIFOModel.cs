using System;

namespace MISReports_Api.Models.FIFO
{
    public class PHVObsoleteIdleFIFOModel
    {
        public string DocumentNo { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string GradeCode { get; set; }
        public DateTime? PhvDate { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal StockBook { get; set; }
        public string Reason { get; set; }
        public string CostCentreName { get; set; }
    }
}