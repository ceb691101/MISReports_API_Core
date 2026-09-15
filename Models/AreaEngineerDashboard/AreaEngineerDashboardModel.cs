using System.Collections.Generic;

namespace MISReports_Api.Models.AreaEngineerDashboard
{
    public class AreaQtyItem
    {
        public string areaId { get; set; }
        public string areaName { get; set; }
        public double qtyOnHand { get; set; }
        public double stockValue { get; set; }
    }

    public class AreaEngineerMaterialMasterItem
    {
        public string matCd { get; set; }
        public string matNm { get; set; }
        public string uomCd { get; set; }
        public double unitPrice { get; set; }
        public double provinceQtyOnHand { get; set; }
        public double provinceStockValue { get; set; }
        public List<AreaQtyItem> areaBreakdown { get; set; } = new List<AreaQtyItem>();
    }

    public class AreaEngineerMaterialMasterSummaryModel
    {
        public string provinceId { get; set; }
        public string provinceName { get; set; }
        public double totalProvinceQtyOnHand { get; set; }
        public double totalProvinceStockValue { get; set; }
        public List<AreaQtyItem> areaTotals { get; set; } = new List<AreaQtyItem>();
        public List<AreaEngineerMaterialMasterItem> materials { get; set; } = new List<AreaEngineerMaterialMasterItem>();
    }
}
