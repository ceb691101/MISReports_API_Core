using System;

namespace MISReports_Api.Models.PIV
{
    public class AreaWiseSRPApplicationPIVPaidReportModel
    {
        public string Division { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }
        public string CCT_NAME { get; set; }

        public string DeptId { get; set; }
        public string IdNo { get; set; }
        public string ApplicationNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }

        public DateTime? SubmitDate { get; set; }
        public string Description { get; set; }

        public string PivNo { get; set; }
        public DateTime? PaidDate { get; set; }
        public decimal? PivAmount { get; set; }

        public string TariffCode { get; set; }
        public string Phase { get; set; }
        public string ExistingAccNo { get; set; }
    }
}