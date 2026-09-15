using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class ChequeSummaryModel
    {
        public DateTime? ChqDt { get; set; }
        public string ChqNo { get; set; }
        public decimal? ChqAmt { get; set; }
        public string CctName { get; set; }
    }
}