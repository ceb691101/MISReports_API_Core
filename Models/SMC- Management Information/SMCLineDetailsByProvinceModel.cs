using System;

namespace MISReports_Api.Models.Accounts
{
    public class SMCLineDetailsByProvinceModel
    {
        public string DeptId { get; set; }

        public string Phase { get; set; }

        public string ConnectionType { get; set; }

        public string TariffCatCode { get; set; }

        public string LoopCable { get; set; }

        public string WiringType { get; set; }

        public DateTime? PrjAssDt { get; set; }

        public string ProjectNo { get; set; }

        public string LineLength { get; set; }

        public decimal? ActualCost { get; set; }

        public decimal? StandardCost { get; set; }

        public string CompNm { get; set; }
    }
}

