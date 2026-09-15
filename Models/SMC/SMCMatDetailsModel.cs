using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class SMCMatDetailsModel
    {
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public string TariffCatCode { get; set; }
        public string LoopCable { get; set; }
        public string WiringType { get; set; }
        public decimal? ActualCost { get; set; }
        public decimal? StandardCost { get; set; }
        public string ProjectNo { get; set; }
        public string ResCd { get; set; }
        public decimal? Qty { get; set; }
        public string MatNm { get; set; }
        public string MajUom { get; set; }
        public string Area { get; set; }
        public string CompNm { get; set; }
    }
}