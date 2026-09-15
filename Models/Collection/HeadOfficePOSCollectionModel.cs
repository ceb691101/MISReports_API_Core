namespace MISReports_Api.Models.Collection
{
    public class HeadOfficePOSCollectionModel
    {
        public string AreaName { get; set; }
        public int Count { get; set; }
        public decimal SumTransAmt { get; set; }
        public string AreaCode { get; set; }
    }

    public class HeadOfficePOSCollectionRequest
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string ReportType { get; set; } // "Bulk" or "Ordinary"
    }
}
