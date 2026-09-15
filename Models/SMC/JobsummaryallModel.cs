using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class JobSummaryAllModel
    {
        public string DeptId { get; set; }
        public string EstimateNo { get; set; }
        public string ProjectNo { get; set; }
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public decimal? StdCost { get; set; }
        public decimal? ActualCost { get; set; }
        public string Descr { get; set; }
    }
}