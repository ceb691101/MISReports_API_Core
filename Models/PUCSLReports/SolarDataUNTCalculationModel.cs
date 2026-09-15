namespace MISReports_Api.Models.PUCSLReports
{
    public class SolarDataUNTCalculationModel
    {
        public string Category { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }
        public int Accts { get; set; }
        public decimal UnitsExpD { get; set; }
        public decimal UnitsExpP { get; set; }
        public decimal UnitsExpOffP { get; set; }
        public decimal UnitsImpD { get; set; }
        public decimal UnitsImpP { get; set; }
        public decimal UnitsImpOffP { get; set; }
    }

    public class SolarDataUNTCalculationResponse
    {
        public System.Collections.Generic.List<SolarDataUNTCalculationModel> Data { get; set; }
        public SolarDataUNTCalculationModel Total { get; set; }
        public string ErrorMessage { get; set; }
    }
}
