//20. PIV Details (Issued and Paid Cost Centers AFMHQ Only)

using System;

namespace MISReports_Api.Models.PIV
{
    public class AccountCodesWisePivModel
    {
        public string DeptId { get; set; }
        public string PivNo { get; set; }
        public string PivReceiptNo { get; set; }
        public DateTime? PivDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string AccountCode { get; set; }
        public decimal? Amount { get; set; }

        // From subquery
        public string CctName { get; set; }     // department name of the PIV department (dept_id)

        public string CctName1 { get; set; }    // department name of the :costctr (paid_dept_id)
    }
}