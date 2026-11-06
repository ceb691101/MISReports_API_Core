using System.Collections.Generic;

namespace MISReports_Api.Models.Inventory
{
    public class QuantityOnHandGradeCodewiseModel
    {
        public string WarehouseCode { get; set; }

        public string MaterialCode { get; set; }

        public string MaterialName { get; set; }

        public string GradeCode { get; set; }

        public string MajorUom { get; set; }

        public decimal QuantityOnHand { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Value { get; set; }

        public string Status { get; set; }

        public string CostCenterName { get; set; }
    }

    public class QuantityOnHandGradeCodewiseResponse
    {
        public List<QuantityOnHandGradeCodewiseModel> Data { get; set; }

        public string ErrorMessage { get; set; }

        public string ErrorDetails { get; set; }
    }
}