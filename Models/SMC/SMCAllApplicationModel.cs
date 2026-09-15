using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class SMCAllApplicationModel
    {
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
        public string ProjectNo { get; set; }
        public string AccountNo { get; set; }
        public string Area { get; set; }
        public string Province { get; set; }
        public string CctName { get; set; }
        public string CompNm { get; set; }
    }
}