namespace MISReports_Api.Models
{
    public class MaterialCommittedStockModel
    {
        public string DeptId { get; set; }
        public string WrhCd { get; set; }
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string GradeCd { get; set; }
        public string Major { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Value { get; set; }
    }

    public class MaterialCommittedStockProvinceModel
    {
        public string CompId { get; set; }
        public string CompNm { get; set; }
    }
}