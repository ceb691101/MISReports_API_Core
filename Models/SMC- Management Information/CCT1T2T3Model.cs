using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class CCT1T2T3Model
    {
        public string ApplicationNo { get; set; }
        public string ApplicationId { get; set; }
        public string ProjectNo { get; set; }
        public DateTime? AccCreatedDate { get; set; }
        public DateTime? Piv1Date { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public decimal? EstimateCost { get; set; }
        public DateTime? Piv2Date { get; set; }
        public DateTime? EnergizedDate { get; set; }
        public decimal? T1 { get; set; }
        public decimal? T2Ln { get; set; }
        public decimal? T2Smc { get; set; }
        public decimal? T3 { get; set; }
        public string Loan { get; set; }
        public string CctName { get; set; }
    }
}