using System;

namespace MISReports_Api.Models
{
    public class LastDocModel
    {
        public string DocPrefix { get; set; }
        public string MaxDocNo { get; set; }
        public DateTime? MaxTrxDate { get; set; }
        public string CostCenterName { get; set; }
    }
}