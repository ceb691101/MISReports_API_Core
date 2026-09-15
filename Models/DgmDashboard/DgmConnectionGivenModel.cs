using System;

namespace MISReports_Api.Models.DgmDashboard
{
    public class DgmConnectionGivenModel
    {
        public string deptId { get; set; }
        public string description { get; set; }
        public string appType { get; set; }
        public int noOfConnections { get; set; }
    }
}
