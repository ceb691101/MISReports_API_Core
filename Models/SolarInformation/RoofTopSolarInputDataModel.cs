using System;

namespace MISReports_Api.Models.SolarInformation
{
    public class RoofTopSolarInputDataModel
    {
        public int ScenarioNumber { get; set; }
        public string ScenarioDescription { get; set; }

        // B/F column (scenarios 1, 6, 7, 8)
        public long? BF { get; set; }

        // Σ(Export-Import) - first summation column (scenarios 2, 3, 5)
        public long? SumExportMinusImport1 { get; set; }

        // Σ(Export-Import) - second summation column (scenarios 4, 5)
        public long? SumExportMinusImport2 { get; set; }

        // Σ Export column (scenarios 6, 7, 8)
        public long? SumExport { get; set; }

        // Metadata
        public string CalcCycle { get; set; }
        public string ErrorMessage { get; set; }
    }
}