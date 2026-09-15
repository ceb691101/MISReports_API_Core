//22. Refunded PIV Details
using System;

namespace MISReports_Api.Models.PIV
{
    public class RefundedPivModel
    {
        public string DeptId { get; set; }
        public string TitleCd { get; set; }          // actually title_nm from subquery
        public DateTime? PivDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string PivNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public decimal? GrandTotal { get; set; }
        public decimal? RefundableAmount { get; set; }
        public DateTime? RefundDate { get; set; }
        public string AccountCode { get; set; }
        public string CctName { get; set; }          // department name of :costctr
    }
}