using System;
using System.Collections.Generic;
using MISReports_Api.Models.DgmDashboard;
using MISReports_Api.Models.FinancialDashboard;

namespace MISReports_Api.Models.IntegratedDashboard
{
    public class IntegratedConstructionSummaryModel
    {
        public string ProvinceCode { get; set; }
        public int Year { get; set; }
        public double StockValue { get; set; }
        public List<PivTotalModel> PivTotal { get; set; } = new List<PivTotalModel>();
        public double PivPeriodSummary { get; set; }
        public List<DgmAppCountModel> ApplicationCounts { get; set; } = new List<DgmAppCountModel>();
        public List<DgmConnectionGivenModel> ConnectionsGiven { get; set; } = new List<DgmConnectionGivenModel>();
        public List<DgmPendingApplicationModel> PendingApplications { get; set; } = new List<DgmPendingApplicationModel>();
        public DateTimeOffset FetchedAt { get; set; }
    }
}
