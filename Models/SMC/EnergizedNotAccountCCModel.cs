using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class EnergizedNotAccountCCModel
    {
        public DateTime? SubmitDate { get; set; }
        public string ApplicationId { get; set; }
        public DateTime? PivDate1 { get; set; }
        public string ApplicationNo { get; set; }
        public DateTime? PivDate2 { get; set; }
        public DateTime? PivDate21 { get; set; }
        public DateTime? AllocatedDate { get; set; }
        public string ProjectNo { get; set; }
        public string IsLoanApp { get; set; }
        public string MeterNo1 { get; set; }
        public string CctName { get; set; }
        public DateTime? ConnectedDate { get; set; }
        public DateTime? SentForBilling { get; set; }
    }
}