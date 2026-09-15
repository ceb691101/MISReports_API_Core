// File: Models/ProvincePIVAllModel.cs
using System;

namespace MISReports_Api.Models
{
    public class ProvincePIVAllModel
    {
        public string Paid_Dept_Id { get; set; }
        public string Dept_Id { get; set; }
        public string Piv_No { get; set; }
        public string Piv_Receipt_No { get; set; }
        public DateTime? Piv_Date { get; set; }
        public DateTime? Paid_Date { get; set; }
        public string Payment_Mode { get; set; }
        public string Cheque_No { get; set; }
        public decimal Grand_Total { get; set; }
        public string Account_Code { get; set; }
        public decimal Amount { get; set; }
        public string Bank_Check_No { get; set; }
        public string CCT_NAME { get; set; }     // Department Name (from dept_id)
        public string CCT_NAME1 { get; set; }    // Company Name (from :compId)
    }
}