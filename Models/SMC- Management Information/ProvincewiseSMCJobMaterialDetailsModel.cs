
using System;

namespace MISReports_Api.Models.SMC__Management_Information
{
    public class ProvincewiseSMCJobMaterialDetailsModel
    {
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public string TariffCatCode { get; set; }
        public string LoopCable { get; set; }
        public string WiringType { get; set; }

        public decimal? LineLength { get; set; }
        public decimal? ServiceLength { get; set; }

        public decimal? ActualCost { get; set; }
        public decimal? StandardCost { get; set; }

        public string ProjectNo { get; set; }
        public string ResCd { get; set; }
        public decimal Qty { get; set; }

        public string MaterialName { get; set; }
        public string MajorUom { get; set; }

        public string Area { get; set; }
        public string CompName { get; set; }
    }
}
