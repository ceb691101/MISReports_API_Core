using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class JobRegisterCCModel
    {
        public string ApplicationNo { get; set; }
        public string ApplicationSubType { get; set; }
        public string ProjectNo { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phase { get; set; }
        public string ConnectionType { get; set; }
        public string TariffCatCode { get; set; }
        public string TariffCode { get; set; }
        public string ServiceStreetAddress { get; set; }
        public string ServiceSuburb { get; set; }
        public string ServiceCity { get; set; }
        public decimal? StdCost { get; set; }
        public string Descr { get; set; }
        public DateTime? PrjAssDt { get; set; }
        public string CctName { get; set; }
    }
}