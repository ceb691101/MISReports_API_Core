using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class GovernmentSectorModel
    {
        public string CatCode { get; set; }
        public string CatDesc { get; set; }
    }

    public class GovernmentLocationModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class GovernmentAgeFilters
    {
        public List<GovernmentSectorModel> sectors { get; set; }
        public List<string> billCycles { get; set; }
    }

    public class GovernmentAgeAmounts
    {
        public decimal BalanceMonthEnd { get; set; }
        public decimal Age0 { get; set; }
        public decimal Age1 { get; set; }
        public decimal Age2 { get; set; }
        public decimal Age3 { get; set; }
        public decimal Age4 { get; set; }
        public decimal Age5 { get; set; }
        public decimal Age6 { get; set; }
        public decimal Age7 { get; set; }
        public decimal Age8 { get; set; }
        public decimal Age9 { get; set; }
        public decimal Age10 { get; set; }
        public decimal Age11 { get; set; }
        public decimal Age12 { get; set; }
        public decimal Over12 { get; set; }
        public decimal Over24 { get; set; }
        public decimal Over36 { get; set; }
    }

    public class GovernmentAgeDetail : GovernmentAgeAmounts
    {
        public string AreaName { get; set; }
        public string SectorName { get; set; }
        public string AccountNumber { get; set; }
        public string CustomerName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string CustomerCode { get; set; }
        public string Tariff { get; set; }
        public decimal ContractDemand { get; set; }
        public decimal SecurityDeposit { get; set; }
    }

    public class GovernmentAgeSummary : GovernmentAgeAmounts
    {
        public string CatCode { get; set; }
        public string CatDesc { get; set; }
        public int CustomerCount { get; set; }
    }
}
