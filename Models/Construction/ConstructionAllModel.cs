using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class ConstructionAllModel
    {
        public string FileNo { get; set; }
        public string ProjectNo { get; set; }
        public string FundId { get; set; }
        public string ConBy { get; set; }
        public string SupBy { get; set; }
        public decimal? Cpercentage { get; set; }
        public DateTime? EnterDate { get; set; }
        public string FileRef { get; set; }
        public decimal? StdCost { get; set; }
        public string CodeNumber { get; set; }
        public string CctName { get; set; }
    }
}