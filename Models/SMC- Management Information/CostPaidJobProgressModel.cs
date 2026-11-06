using System;

namespace MISReports_Api.Models.SMC__Management_Information
{
    public class CostPaidJobProgressModel
    {
        public string ApplicationNo { get; set; }

        public DateTime? SubmitDate { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public string PivNo { get; set; }

        public DateTime? PaidDate { get; set; }

        public decimal? PivAmount { get; set; }

        public DateTime? AllocatedDate { get; set; }

        public DateTime? FinishedDate { get; set; }

        public string ContractorId { get; set; }

        public string ProjectNo { get; set; }

        public DateTime? EstimateDate { get; set; }

        public string ReinspectionPivNo { get; set; }

        public DateTime? ReinspectionPivDate { get; set; }

        public DateTime? ProjectAssignedDate { get; set; }

        public string BillNo { get; set; }

        public DateTime? BillDate { get; set; }

        public string CctName { get; set; }
    }
}