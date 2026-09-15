//07.PIV Collections by Peoples Banks

// File: Models/PIV/PivByPeopleBankModel.cs
namespace MISReports_Api.Models.PIV
{
    public class PivByPeopleBankModel
    {
        public string Cost_center_ID { get; set; }
        public string Account_Code { get; set; }
        public decimal Amount { get; set; }
        public string Cost_center_Name { get; set; }
        public string Company_name { get; set; }
    }
}