//12. PIV Search
// File: PivSearchModel.cs
using System;

namespace MISReports_Api.Models.PIV
{
    public class PivSearchModel
    {
        public string Piv_No { get; set; }
        public string Reference_No { get; set; }
        public string Cheque_No { get; set; }
        public decimal? Paid_Amount { get; set; }
        public DateTime? Piv_Date { get; set; }
        public DateTime? Paid_Date { get; set; }
        public string Paid_Dept_Id { get; set; }
        public string Payment_Mode { get; set; }
    }
}