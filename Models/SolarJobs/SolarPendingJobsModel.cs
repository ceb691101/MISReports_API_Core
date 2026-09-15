using System;

namespace MISReports_Api.Models.SolarJobs
{
    public class SolarPendingJobsModel
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
        public string DeptId { get; set; }
        public string CctName { get; set; }
        public string ProvinceName { get; set; }
    }
}
