using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class AvgConsumptionSelectedDataModel
    {
        public string WarehouseCode { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialName { get; set; }
        public string GradeCode { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal TransactionQuantity { get; set; }     // Net Trx (issues - returns)
        public decimal AverageConsumption { get; set; }      // Average per month
        public string CostCenterName { get; set; }
    }

    public class AvgConsumptionSelectedResponse
    {
        public List<AvgConsumptionSelectedDataModel> Data { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetails { get; set; }
    }
}