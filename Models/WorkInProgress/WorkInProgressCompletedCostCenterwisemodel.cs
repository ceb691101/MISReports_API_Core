using System;

namespace MISReports_Api.Models
{
    public class WorkInProgressCompletedCostCenterwiseModel
    {
        public string ProjectNo { get; set; }
        public decimal StdCost { get; set; }
        public string Descr { get; set; }
        public string FundId { get; set; }
        public string AccountCode { get; set; }
        public string CatCd { get; set; }
        public string DeptId { get; set; }
        public string CctName { get; set; }
        public string C8 { get; set; }
        public decimal CommitedCost { get; set; }
        public DateTime? PaidDate { get; set; }
        public string PivReceiptNo { get; set; }
        public string PivNo { get; set; }
        public decimal PivAmount { get; set; }
        public DateTime? ConfDt { get; set; }
    }
}
