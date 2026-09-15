using System;

namespace MISReports_Api.Models
{
    public class ProjectCommitmentSummaryModel
    {
        public string Period { get; set; }
        public decimal? Sum { get; set; }
        public string CctName { get; set; }
    }
}
