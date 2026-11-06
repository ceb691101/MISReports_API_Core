using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.SolarInformation
{
    public enum SolarRooftopCustomerType
    {
        Ordinary,
        Bulk
    }

    public enum SolarRooftopReportType
    {
        Payments,
        ExportUnits
    }

    public class SolarRooftopPaymentsandExportUnitsRequest
    {
        public string CustomerType { get; set; }
        public string BillCycle { get; set; }
        public string Province { get; set; }
        public string Region { get; set; }
        public string Area { get; set; }
        public SolarRooftopReportType ReportType { get; set; }
    }

    public class SolarRooftopPaymentsandExportUnitsModel
    {
        public string NetType { get; set; }
        public string Province { get; set; }
        public string Region { get; set; }
        public string Area { get; set; }
        public string TariffCategory { get; set; }
        public string TariffCode { get; set; }
        public string CalcCycle { get; set; }
        public string BillCycle { get; set; }
        public decimal Rate { get; set; }
        public decimal Units { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal ExportUnits { get; set; }
        public string ErrorMessage { get; set; }
    }
}