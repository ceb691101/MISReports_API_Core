//Area-wise Solar Sent to Billing Details

using System;

namespace MISReports_Api.Models
{
    public class SolarBillingModel
    {
        public string Dept_Id { get; set; }
        public string Id_No { get; set; }
        public string Application_No { get; set; }
        public string Application_Sub_Type { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public DateTime? Submit_Date { get; set; }
        public decimal? Capacity { get; set; }
        public string Account_No { get; set; }
        public DateTime? Exported_Date { get; set; }
        public string Tariff_Code { get; set; }
        public string Phase { get; set; }
        public string Project_No { get; set; }
        public DateTime? Agreement_Date { get; set; }
        public string Bank_Account_No { get; set; }
        public string Bank_Code { get; set; }
        public string Branch_Code { get; set; }
        public string Cost_Center_Name { get; set; }
        public string Company_Name { get; set; }
    }
}