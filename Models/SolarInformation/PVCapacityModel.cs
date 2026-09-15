namespace MISReports_Api.Models.SolarInformation
{
    public class PVCapacityModel
    {
        public string NetType { get; set; }
        public string Division { get; set; }
        public string Province { get; set; }
        public string Area { get; set; }
        public int NoOfConsumers { get; set; }
        public decimal Capacity { get; set; }
    }
}