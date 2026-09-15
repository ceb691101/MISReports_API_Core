using System.Collections.Generic;

namespace MISReports_Api.Models.Shared
{
    public class BillCycleModel
    {
        public string MaxBillCycle { get; set; }
        public List<string> BillCycles { get; set; } = new List<string>();
        public string ErrorMessage { get; set; }
    }
}