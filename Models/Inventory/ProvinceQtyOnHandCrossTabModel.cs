using System;

namespace MISReports_Api.Models.Inventory
{
    public class ProvinceQtyOnHandCrossTabModel
    {
        public string MAT_CD { get; set; }
        public string MAT_NM { get; set; }

        public decimal Committed_Cost { get; set; }

        public string C8 { get; set; }      // dept_id - dept_nm
        public string Area { get; set; }

        public string UOM_CD { get; set; }

        public string Region { get; set; }
    }
}