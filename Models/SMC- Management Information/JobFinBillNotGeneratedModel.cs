using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class JobFinBillNotGeneratedModel
    {
        public string EstimateNo { get; set; }
        public string ProjectNo { get; set; }
        public string FundId { get; set; }
        public string CatCd { get; set; }
        public DateTime? PrjAssDt { get; set; }
        public DateTime? FinishedDate { get; set; }
        public string ConsumerName { get; set; }
        public string Contractor { get; set; }
        public string CctName { get; set; }
    }
}