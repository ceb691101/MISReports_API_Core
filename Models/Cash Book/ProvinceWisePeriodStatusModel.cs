// Models/ProvinceWisePeriodStatusModel.cs
namespace MISReports_Api.Models
{
    public class ProvinceWisePeriodStatusModel
    {
        public string DeptId { get; set; }
        public string DeptNm { get; set; }
        public string CompId { get; set; }
        public int? FinYear { get; set; }
        public int? FinPrd { get; set; }
        public string Status { get; set; }
        public string CompNm { get; set; }
    }
}