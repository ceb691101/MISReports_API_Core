namespace MISReports_Api.Models.Accounts
{
    public class QtyOnHandReorderModel
    {
        public string WrhCd { get; set; }
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public decimal? QtyOnHand { get; set; }
        public decimal? ReordQty { get; set; }
        public string UomCd { get; set; }
        public string Ref1 { get; set; }
        public decimal? MinStock { get; set; }
        public string BldgNo { get; set; }
        public string ColumnNo { get; set; }
        public string ShelfNo { get; set; }
        public string BinNo { get; set; }
        public string BranchName { get; set; }
    }
}