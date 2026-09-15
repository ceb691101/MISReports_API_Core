using System;

namespace MISReports_Api.Models.Inventory
{
    public class QtyonHandAllRegionModel
    {
        public string MAT_CD { get; set; }
        public string REGION { get; set; }
        public string C8 { get; set; }
        public string DEPT_ID { get; set; }
        public string MAT_NM { get; set; }
        public decimal UNIT_PRICE { get; set; }
        public decimal COMMITED_COST { get; set; }
        public string UOM_CD { get; set; }
    }
}