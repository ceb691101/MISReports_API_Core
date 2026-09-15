using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class JobStatusDocInquiryModel
    {
        public string Status { get; set; }
        public string FundId { get; set; }
        public string ApplicationId { get; set; }
        public string ProjectNo { get; set; }
        public string EstimateNo { get; set; }

        public string CatCd { get; set; }

        public decimal? StdCost { get; set; }
        public decimal? TotalCost { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string TranStatus { get; set; }
        public string CctName { get; set; }
    }
}