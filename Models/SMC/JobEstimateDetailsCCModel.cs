using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class JobEstimateDetailsCCModel
    {
        public string EstimateNo { get; set; }
        public decimal? StdCost { get; set; }
        public string ProjectNo { get; set; }
        public DateTime? PrjAssDt { get; set; }
        public string Descr { get; set; }
        public string ResCd { get; set; }
        public decimal? EstimateQty { get; set; }
        public string MtNm { get; set; }
        public string CctName { get; set; }
        public string Status { get; set; }
    }
}