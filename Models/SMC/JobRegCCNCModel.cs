using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class JobRegCCNCModel
    {
        public string ApplicationNo { get; set; }
        public string PivReceiptNo { get; set; }
        public string PivNo { get; set; }
        public decimal? PivAmount { get; set; }
        public string AccountCode { get; set; }
        public decimal? Amount { get; set; }
        public string Name { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string City { get; set; }
        public DateTime? PaidDate { get; set; }
        public string TariffCatCode { get; set; }
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public string ProjectNo { get; set; }
        public DateTime? AllocatedDate { get; set; }
        public string ContractorId { get; set; }
        public DateTime? FinishedDate { get; set; }
        public string ContractorName { get; set; }
        public string ConfDt { get; set; }
        public string AccNo { get; set; }
        public DateTime? AccDate { get; set; }
        public string CctName { get; set; }
    }
}