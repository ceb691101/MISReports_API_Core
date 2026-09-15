using System;

namespace MISReports_Api.Models.PIV
{
    public class TypewisePIVModel
    {
        public string Dept_Id { get; set; }
        public string Title_Cd { get; set; }                 // This will hold the title_nm from subquery
        public string Reference_No { get; set; }
        public DateTime? Piv_Date { get; set; }
        public DateTime? Paid_Date { get; set; }
        public string Piv_No { get; set; }
        public string Cus_Vat_No { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Telephone_No { get; set; }
        public string Collect_Person_Id { get; set; }
        public string Collect_Person_Name { get; set; }
        public string Description { get; set; }
        public decimal Grand_Total { get; set; }
        public string Vat_Reg_No { get; set; }
        public string Payment_Mode { get; set; }
        public string Cheque_No { get; set; }
        public string Cct_Name { get; set; }
    }
}