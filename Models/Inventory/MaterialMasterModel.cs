using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Inventory
{
    public class MaterialMasterModel
    {
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string MajUom { get; set; }
        public decimal? UnitPrice { get; set; }
        public string Status { get; set; }
    }
}
