namespace MISReports_Api.Models
{
    public class DivisionWiseSRPEstimationModel
    {
        public string Division { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }
        public string CctName { get; set; }
        public string CompName { get; set; }

        public string DeptId { get; set; }
        public string IdNo { get; set; }
        public string ApplicationNo { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }

        public System.DateTime? SubmitDate { get; set; }
        public string Description { get; set; }

        public string PivNo { get; set; }
        public System.DateTime? PaidDate { get; set; }
        public decimal PivAmount { get; set; }

        public string TariffCode { get; set; }
        public string Phase { get; set; }
        public string ExistingAccNo { get; set; }
    }
}