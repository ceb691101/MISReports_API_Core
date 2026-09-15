//07.2 PIV Collections by IPG ( SLT )

// File: Models/PivBySLTModels.cs
namespace MISReports_Api.Models.PIV
{
    public class PivBySLTModel
    {
        public string Cost_center_ID { get; set; }
        public string Account_Code { get; set; }
        public decimal Amount { get; set; }
        public string Cost_center_Name { get; set; }
        public string Company_name { get; set; }
    }
}