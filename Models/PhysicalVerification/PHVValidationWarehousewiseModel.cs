namespace MISReports_Api.Models.PhysicalVerification
{
    public class PHVValidationWarehousewiseModel
    {
        public string WarehouseCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string UomCode { get; set; }
        public string GradeCode { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal CountedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public string Reason { get; set; }
    }
}
