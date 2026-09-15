using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class PendingEstimationCCModel
    {
        public string ApplicationType { get; set; }
        public string ApplicationSubType { get; set; }
        public string ApplicationNo { get; set; }
        public string ApplicationId { get; set; }
        public string Name { get; set; }
        public string CusAddress { get; set; }
        public DateTime? SubmitDate { get; set; }
        public string Status { get; set; }
        public string TariffCode { get; set; }
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public string CctName { get; set; }
    }
}