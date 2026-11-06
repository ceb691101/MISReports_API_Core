namespace MISReports_Api.Models.Accounts
{
    public class SmcLineDetailsModel
    {
        public string CompNm { get; set; }
        public string Area { get; set; }
        public string DeptId { get; set; }
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public string TariffCatCode { get; set; }
        public string LoopCable { get; set; }
        public string WiringType { get; set; }
        public decimal? LineLength { get; set; }
        public decimal? ServiceLength { get; set; }
        public decimal? InsideLength { get; set; }
        public decimal? ActualCost { get; set; }
        public decimal? StandardCost { get; set; }
        public string ProjectNo { get; set; }
    }
}