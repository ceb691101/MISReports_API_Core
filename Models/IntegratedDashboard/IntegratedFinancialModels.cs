using System;
using System.Collections.Generic;
using MISReports_Api.Models.FinancialDashboard;

namespace MISReports_Api.Models.IntegratedDashboard
{
    public class IntegratedFinancialSummaryModel
    {
        public List<PivTotalModel> PivTotal { get; set; } = new List<PivTotalModel>();
        public List<PivDivisionModel> PivDivision { get; set; } = new List<PivDivisionModel>();
        public double StockTotal { get; set; }
        public List<StockDivisionModel> StockDivision { get; set; } = new List<StockDivisionModel>();
        public double Total7DayCollection { get; set; }
        public string LatestPivDate { get; set; }
        public double LatestPivTotal { get; set; }
        public DateTimeOffset FetchedAt { get; set; }
        public string AppliedDivision { get; set; }
        public string AppliedProvince { get; set; }
        public string AppliedArea { get; set; }
    }

    public class IntegratedPivItemModel
    {
        public string Date { get; set; }
        public string Company { get; set; }
        public string Division { get; set; }
        public double Amount { get; set; }
    }

    public class IntegratedStockItemModel
    {
        public string Company { get; set; }
        public string Division { get; set; }
        public double Amount { get; set; }
    }
}
