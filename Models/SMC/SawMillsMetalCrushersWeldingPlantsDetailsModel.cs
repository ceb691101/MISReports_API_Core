using System;

namespace MISReports_Api.Models.SMC
{
    public class SawMillsMetalCrushersWeldingPlantsDetailsModel
    {
        public DateTime? PrjAssDt { get; set; }

        public string ProjectNo { get; set; }

        public string Phase { get; set; }

        public string ConnectionType { get; set; }

        public string TariffCatCode { get; set; }

        public decimal? LineLength { get; set; }

        public decimal? ServiceLength { get; set; }

        public decimal? InsideLength { get; set; }

        public decimal? ActualCost { get; set; }

        public decimal? ConsumerPayable { get; set; }

        public string DeptId { get; set; }

        public decimal? WeldingPlant { get; set; }

        public decimal? MetalCrusher { get; set; }

        public decimal? SawMills { get; set; }
    }
}