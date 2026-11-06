using System;

namespace MISReports_Api.Models.Inventory
{
    public class QuantityOnHandLessThanReorderLevelModel
    {
        public string WarehouseCode { get; set; }

        public string MaterialCode { get; set; }

        public string MaterialName { get; set; }

        public decimal? QuantityOnHand { get; set; }

        public decimal? ReorderQuantity { get; set; }

        public string UomCode { get; set; }

        public string Reference { get; set; }

        public decimal? MinimumStock { get; set; }

        public string CostCentreName { get; set; }
    }
}