using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class JobSummaryPeriodModel
    {
        public DateTime? PrjAssDt { get; set; }
        public string ProjectNo { get; set; }
        public string EstimateNo { get; set; }
        public decimal? StandardCost { get; set; }
        public decimal? EstimateCost { get; set; }

        public decimal? Actual { get; set; }

        public string Descr { get; set; }
        public string CctName { get; set; }
    }
}