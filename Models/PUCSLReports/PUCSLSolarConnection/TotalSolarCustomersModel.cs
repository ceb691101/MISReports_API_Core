using System;
using System.Collections.Generic;

namespace MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection
{
    /// <summary>
    /// Model for Total No of Solar Customers Report (Report 3).
    /// Returns separate Ordinary and Bulk sections.
    /// </summary>
    public class TotalSolarCustomersResponse
    {
        public List<OrdinaryData> Ordinary { get; set; }
        public OrdinaryData OrdinaryTotal { get; set; }
        public List<BulkData> Bulk { get; set; }
        public BulkData BulkTotal { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Ordinary customer data by tariff class.
    /// </summary>
    public class OrdinaryData
    {
        public string TariffCategory { get; set; }

        // Net Metering (net_type='1')
        public int NetMeteringCustomers { get; set; }
        public decimal NetMeteringUnits { get; set; }

        // Net Accounting (net_type='2' OR net_type='5')
        public int NetAccountingCustomers { get; set; }
        public decimal NetAccountingUnits { get; set; }

        // Net Plus (net_type='3')
        public int NetPlusCustomers { get; set; }
        public decimal NetPlusUnits { get; set; }

        // Net Plus Plus (net_type='4')
        public int NetPlusPlusCustomers { get; set; }
        public decimal NetPlusPlusUnits { get; set; }
    }

    /// <summary>
    /// Bulk customer data by tariff code.
    /// </summary>
    public class BulkData
    {
        public string TariffCategory { get; set; }

        // Net Metering (net_type='1')
        public int NetMeteringCustomers { get; set; }
        public decimal NetMeteringUnits { get; set; }

        // Net Accounting (net_type='2')
        public int NetAccountingCustomers { get; set; }
        public decimal NetAccountingUnits { get; set; }

        // Net Plus (net_type='3')
        public int NetPlusCustomers { get; set; }
        public decimal NetPlusUnits { get; set; }

        // Net Plus Plus (net_type='4')
        public int NetPlusPlusCustomers { get; set; }
        public decimal NetPlusPlusUnits { get; set; }
    }
}