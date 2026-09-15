using System;

namespace MISReports_Api.Models
{
    public class ProvincePivOtherCCModel
    {
        public string Paid_Dept_Id { get; set; }              // paid_dept_id
        public string Dept_Id { get; set; }
        public string Piv_No { get; set; }
        public string Piv_Receipt_No { get; set; }
        public DateTime? Piv_Date { get; set; }
        public DateTime? Paid_Date { get; set; }
        public string Payment_Mode { get; set; }
        public string Cheque_No { get; set; }
        public decimal Grand_Total { get; set; }
        public string Account_Code { get; set; }               // account_code
        public decimal Amount { get; set; }
        public string Bank_Check_No { get; set; } = string.Empty;
        public string CCT_NAME { get; set; } = string.Empty;         // Department name
        public string CCT_NAME1 { get; set; } = string.Empty;        // Company name
    }
}