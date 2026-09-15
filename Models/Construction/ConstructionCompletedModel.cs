using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class ConstructionCompletedModel
    {
        public string District { get; set; }
        public string ServiceDepoName { get; set; }
        public string Electorate { get; set; }
        public string Descr { get; set; }
        public decimal? StdCost { get; set; }
        public decimal? CPercentage { get; set; }
        public decimal? Wp { get; set; }
        public string ProjectNo { get; set; }
        public string FileNo { get; set; }
        public string Remarks { get; set; }
        public DateTime? CompDate { get; set; }
        public string CctName { get; set; }
    }
}