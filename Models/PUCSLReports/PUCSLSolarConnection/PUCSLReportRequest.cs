namespace MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection
{
    /// <summary>
    /// Net-metering scheme type sent from the frontend SolarType selector.
    /// Maps to Informix net_type column values:
    ///   NetAccounting → net_type IN ('2','5')
    ///   NetPlus       → net_type = '3'
    ///   NetPlusPlus   → net_type = '4'
    /// </summary>
    public enum SolarNetType
    {
        NetMetering,
        NetAccounting,
        NetPlus,
        NetPlusPlus
    }

    /// <summary>
    /// Geographic scope of a PUCSL report.
    /// </summary>
    public enum PUCSLReportCategory
    {
        Province,
        Region,
        EntireCEB
    }

    /// <summary>
    /// Which PUCSL sub-report to generate.
    /// </summary>
    public enum PUCSLReportType
    {
        FixedSolarData,
        VariableSolarData,
        TotalSolarCustomers,
        RawDataForSolar,
        NetMetering
    }

    // ====================================================================
    //  Internal DAO request model
    //  (Built by the controller from the raw PUCSLReportRequest POST body
    //   and passed directly to FixedSolarDataDao / VariableSolarDataDao.)
    // ====================================================================

    /// <summary>
    /// Strongly-typed request object consumed by PUCSL DAOs.
    /// The controller is responsible for parsing the incoming strings
    /// (from PUCSLReportRequest) into the typed enum fields here.
    /// </summary>
    public class PUCSLRequest
    {
        /// <summary>Geographic scope: Province, Region, or EntireCEB.</summary>
        public PUCSLReportCategory ReportCategory { get; set; }

        /// <summary>
        /// prov_code when ReportCategory = Province;
        /// region code when ReportCategory = Region;
        /// null / empty for EntireCEB.
        /// </summary>
        public string TypeCode { get; set; }

        /// <summary>Bill / calculation cycle, e.g. "202501".</summary>
        public string BillCycle { get; set; }

        /// <summary>Which sub-report to run.</summary>
        public PUCSLReportType ReportType { get; set; }

        /// <summary>Net-metering scheme filter.</summary>
        public SolarNetType SolarType { get; set; }
    }
}