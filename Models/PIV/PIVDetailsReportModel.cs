// 08. PIV Details Report

using System;

namespace MISReports_Api.Models.PIV
{
    public class PIVDetailsReportModel
    {
        public string Dept_Id { get; set; }              // Issued cost center
        public string Paid_Dept_Id { get; set; }
        public string Piv_No { get; set; }
        public string Piv_Receipt_No { get; set; }
        public DateTime? Piv_Date { get; set; }
        public DateTime? Paid_Date { get; set; }
        public string Payment_Mode { get; set; }
        public decimal Piv_Amount { get; set; }
        public decimal Paid_Amount { get; set; }
        public decimal Difference { get; set; }
        public string Bank_Check_No { get; set; }
        public string Cct_Name { get; set; }             // Issued cost center name
        public string Company { get; set; }
    }
}