using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class CCSolarPendingModel
    {
        public string ApplicationId { get; set; }
        public string ApplicationNo { get; set; }
        public DateTime? SubmitDate { get; set; }
        public string ProjectNo { get; set; }
        public DateTime? PivDate { get; set; }
        public string ApplicationSubType { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime? Piv2PaidDate { get; set; }
        public string ExistingAccNo { get; set; }
        public string Status { get; set; }
        public string CctName { get; set; }
    }
}