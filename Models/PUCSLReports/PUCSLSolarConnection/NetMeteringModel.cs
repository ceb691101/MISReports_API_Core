using System.Collections.Generic;

namespace MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection
{
    /// <summary>
    /// Single row in the Net Metering report.
    /// Represents aggregated data for a tariff category (D, GP, H, I, R, GV).
    /// Combines both Ordinary and Bulk customers/units.
    /// </summary>
    public class NetMeteringData
    {
        /// <summary>Tariff category code (e.g., "D", "GP", "H", "I", "R", "GV")</summary>
        public string Category { get; set; }

        /// <summary>Year as 2-digit string (e.g., "25")</summary>
        public string Year { get; set; }

        /// <summary>Month as number string (e.g., "9" for September)</summary>
        public string Month { get; set; }

        /// <summary>Total number of customers (Ordinary + Bulk)</summary>
        public int NoOfCustomers { get; set; }

        /// <summary>Total units in kWh (Ordinary units_out + Bulk exp_kwd_units)</summary>
        public decimal UnitsDayKwh { get; set; }

        /// <summary>Units Peak (Bulk only - exp_kwp_units)</summary>
        public decimal UnitsPeakKwh { get; set; }

        /// <summary>Units Off-Peak (Bulk only - exp_kwo_units)</summary>
        public decimal UnitsOffPeakKwh { get; set; }
    }

    /// <summary>
    /// Response wrapper for Net Metering report.
    /// Contains list of category rows and a total row.
    /// </summary>
    public class NetMeteringResponse
    {
        /// <summary>List of data rows grouped by tariff category</summary>
        public List<NetMeteringData> Data { get; set; }

        /// <summary>Total row summing all categories</summary>
        public NetMeteringData Total { get; set; }

        /// <summary>Error message if any issues occurred</summary>
        public string ErrorMessage { get; set; }

        public NetMeteringResponse()
        {
            Data = new List<NetMeteringData>();
        }
    }
}