//23.Region wise PIV Collections by Provincial POS relevant to Other Cost Centers

using System;

namespace MISReports_Api.Models
{
    public class RegionPivFromOtherCCModel
    {
        public string C6 { get; set; }               // = paid_dept_id
        public string Dept_Id { get; set; }
        public string Piv_No { get; set; }
        public string Piv_Receipt_No { get; set; }
        public DateTime? Piv_Date { get; set; }
        public DateTime? Paid_Date { get; set; }
        public string Payment_Mode { get; set; }
        public string Cheque_No { get; set; }
        public decimal Grand_Total { get; set; }
        public string C8 { get; set; }               // = account_code
        public decimal Amount { get; set; }
        public string Bank_Check_No { get; set; }
        public string CCT_NAME { get; set; }         // paid_dept_id → dept_nm
        public string CCT_NAME1 { get; set; }        // company name from :compId
    }
}