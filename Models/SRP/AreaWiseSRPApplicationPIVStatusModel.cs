using System;

namespace MISReports_Api.Models.SRP
{
    public class AreaWiseSRPApplicationPIVStatusModel
    {
        public string Dept_Id { get; set; }
        public string Id_No { get; set; }
        public string Application_No { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public DateTime? Submit_Date { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string Piv_No { get; set; }
        public DateTime? Paid_Date { get; set; }
        public decimal Piv_Amount { get; set; }
        public string Tariff_Code { get; set; }
        public string Phase { get; set; }
        public string Existing_Acc_No { get; set; }
        public string Area { get; set; }
        public string Province { get; set; }
        public string Cct_Name { get; set; }
        public string Comp_Nm { get; set; }
    }
}