//05.Branch wise PIV Collections by Other Cost Centers relevant to the Province

using System;

namespace MISReports_Api.Models
{
    public class OtherCCtoProvinceModel
    {
        public string Paid_Dept_Id { get; set; }          // Previously C6
        public string Dept_Id { get; set; }
        public string Piv_No { get; set; }
        public string Piv_Receipt_No { get; set; }
        public DateTime? Piv_Date { get; set; }
        public DateTime? Paid_Date { get; set; }
        public string Payment_Mode { get; set; }
        public string Cheque_No { get; set; }
        public decimal Grand_Total { get; set; }
        public string Account_Code { get; set; }          // Previously C8
        public decimal Amount { get; set; }
        public string Bank_Check_No { get; set; }
        public string CCT_NAME { get; set; }              // Receiving department name (dept_nm of c.dept_id)
        public string CCT_NAME1 { get; set; }             // Company name for :compId
    }
}