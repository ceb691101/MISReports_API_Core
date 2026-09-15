using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class PivIIPaidNotEnergizedModel
    {
        public string ApplicationType { get; set; }
        public string ApplicationSubType { get; set; }
        public string TariffCode { get; set; }
        public string Phase { get; set; }
        public decimal? StdCost { get; set; }
        public string EstimateNo { get; set; }
        public string ProjectNo { get; set; }
        public DateTime? ConfirmedDate { get; set; }
        public string PivNo { get; set; }
        public decimal? PaidAmount { get; set; }
        public string CctName { get; set; }
    }
}