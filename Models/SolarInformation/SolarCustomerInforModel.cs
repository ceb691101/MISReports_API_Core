using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.SolarInformation
{
    // Main response model
    public class SolarCustomerInforResponse
    {
        public string CustomerType { get; set; } // "Ordinary" or "Bulk"
        public string AccountNumber { get; set; }
        public SolarCustomerBasicInfo CustomerInfo { get; set; }
        public List<SolarEnergyHistoryModel> EnergyHistory { get; set; }
        public string ErrorMessage { get; set; }

        public SolarCustomerInforResponse()
        {
            EnergyHistory = new List<SolarEnergyHistoryModel>();
        }
    }

    // Table 1: Customer Basic Information
    public class SolarCustomerBasicInfo
    {
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string AgreementDate { get; set; }
        public decimal GenerationCapacity { get; set; }
        public string Bank { get; set; }
        public string Branch { get; set; }
        public string BankAccountNumber { get; set; }
        public string NoOfPhase { get; set; }
        public string ContractDemand { get; set; }
        public string CrntDepot { get; set; }
        public string SubstnCode { get; set; }

        // Additional fields from database
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string TelephoneNumber { get; set; }
        public string MeterNumber1 { get; set; }
        public string MeterNumber2 { get; set; }
        public string MeterNumber3 { get; set; }
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public string Region { get; set; }
        public string TariffCode { get; set; }
        public string NetType { get; set; }
        public string BankCode { get; set; }
        public string BranchCode { get; set; }

    }

    // Table 2: Last 6 Months Energy History
    public class SolarEnergyHistoryModel
    {
        public string CalcCycle { get; set; }
        public string EnergyExported { get; set; } // units_out
        public string EnergyImported { get; set; } // units_in
        public string PresentReadingDate { get; set; }
        public string PreviousReadingDate { get; set; }
        public string PresentReadingExport { get; set; }
        public string PreviousReadingExport { get; set; }
        public string MeterNumber { get; set; }
        public string MeterNumber1 { get; set; }
        public string MeterNumber2 { get; set; }
        public string MeterNumber3 { get; set; }
        public decimal UnitCost { get; set; } // rate or unitsale
        public decimal UnitSale { get; set; }
        public string Kwo { get; set; } 
        public string Kwd { get; set; }  
        public string Kwp { get; set; }  
        public string Kva { get; set; }
        public string ExportKwd { get; set; }
    }
}