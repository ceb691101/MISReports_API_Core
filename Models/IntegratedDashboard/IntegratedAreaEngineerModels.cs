using System;
using System.Collections.Generic;
using MISReports_Api.Models.AreaEngineerDashboard;
using MISReports_Api.Models.DgmDashboard;
using MISReports_Api.Models.FinancialDashboard;

namespace MISReports_Api.Models.IntegratedDashboard
{
    public class IntegratedAreaEngineerSummaryModel
    {
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public int Year { get; set; }
        public double StockValue { get; set; }
        public List<PivTotalModel> PivTotal { get; set; } = new List<PivTotalModel>();
        public double PivPeriodSummary { get; set; }
        public AreaEngineerMaterialMasterSummaryModel MaterialMaster { get; set; }
        public List<DgmAppCountModel> ApplicationCounts { get; set; } = new List<DgmAppCountModel>();
        public List<DgmConnectionGivenModel> ConnectionsGiven { get; set; } = new List<DgmConnectionGivenModel>();
        public List<DgmPendingApplicationModel> PendingApplications { get; set; } = new List<DgmPendingApplicationModel>();
        public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
