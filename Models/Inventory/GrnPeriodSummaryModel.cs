using System;
using System.Collections.Generic;
using System.Linq;

// Models/GrnPeriodSummaryModel.cs
namespace MISReports_Api.Models
{
    // Period-wide stats that do NOT depend on the material-code filter — always "all materials".
    public class GrnPeriodSummaryModel
    {
        public int GrnCount { get; set; }
        public int IssueCount { get; set; }
        public decimal IssueTotal { get; set; }
    }
}