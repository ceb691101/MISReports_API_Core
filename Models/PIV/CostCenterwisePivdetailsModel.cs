//26. C/C PIV Details (Status Report)

using System;

namespace MISReports_Api.Models.PIV
{
    public class CostCenterwisePivdetailsModel
    {
        public string DeptId { get; set; }              // c.dept_id
        public string PivNo { get; set; }               // c.piv_no
        public DateTime? PivDate { get; set; }          // c.piv_date
        public DateTime? PaidDate { get; set; }         // c.paid_date
        public string PaymentMode { get; set; }         // c.payment_mode
        public decimal? PivAmount { get; set; }         // c.grand_total
        public string Status { get; set; }              // piv_activity.description_1
        public string CctName { get; set; }             // gldeptm.dept_nm (for dept_id)
        public string CctName1 { get; set; }            // gldeptm.dept_nm (for empty dept_id → probably placeholder)
    }
}