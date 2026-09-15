using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class SMCJobProgressModel
    {
        public string ApplicationNo { get; set; }
        public DateTime? SubmitDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? Piv2ConfirmedDate { get; set; }
        public decimal? DNoticeDays { get; set; }
        public DateTime? AllocatedDate { get; set; }
        public DateTime? FinishedDate { get; set; }
        public string ProjectNo { get; set; }
        public DateTime? EstimateDt { get; set; }
        public DateTime? PrjAssDt { get; set; }
        public DateTime? EngizedDate { get; set; }
        public string AccNo { get; set; }
        public DateTime? AccDate { get; set; }
        public decimal? T1 { get; set; }
        public decimal? T2Smc { get; set; }
        public decimal? T3 { get; set; }
        public string CctName { get; set; }
    }
}