using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MISReports_Api.DAL.DgmDashboard;
using MISReports_Api.Models.DgmDashboard;
using MISReports_Api.Models.FinancialDashboard;
using MISReports_Api.Models.IntegratedDashboard;

namespace MISReports_Api.DAL.IntegratedDashboard
{
    public class IntegratedConstructionDao
    {
        private static readonly DgmPivTotalDao PivTotalDao = new DgmPivTotalDao();
        private static readonly DgmPivPeriodSummaryDao PivPeriodSummaryDao = new DgmPivPeriodSummaryDao();
        private static readonly DgmStockValueDao StockValueDao = new DgmStockValueDao();
        private static readonly DgmAppCountDao AppCountDao = new DgmAppCountDao();
        private static readonly DgmConnectionGivenDao ConnectionGivenDao = new DgmConnectionGivenDao();
        private static readonly DgmPendingApplicationsDao PendingApplicationsDao = new DgmPendingApplicationsDao();

        public static string ResolveCompanyId(string provinceCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode) || provinceCode.Equals("all", StringComparison.OrdinalIgnoreCase))
                return "WPN";

            var clean = provinceCode.Trim().ToUpper();

            switch (clean)
            {
                case "1":
                case "WPN":
                case "WESTERN PROVINCE NORTH":
                    return "WPN";
                case "2":
                case "WPS":
                case "WPS1":
                case "WPS 1":
                case "WESTERN PROVINCE SOUTH 1":
                case "WESTERN PROVINCE SOUTH I":
                    return "WPS1";
                case "C":
                case "WPS2":
                case "WPSII":
                case "WPS II":
                case "WESTERN PROVINCE SOUTH 2":
                case "WESTERN PROVINCE SOUTH II":
                    return "WPSII";
                case "3":
                case "CC":
                case "COL CITY":
                case "COLOMBO CITY":
                    return "CC";
                case "4":
                case "NP":
                case "NORTHERN":
                case "NORTHERN PROVINCE":
                    return "NP";
                case "5":
                case "CP":
                case "CENTRAL":
                case "CENTRAL PROVINCE":
                    return "CP";
                case "E":
                case "CP2":
                case "CENTRAL 2":
                case "CENTRAL II":
                    return "CP2";
                case "6":
                case "UVA":
                case "UVAP":
                case "UVA PROVINCE":
                    return "UVAP";
                case "7":
                case "EP":
                case "EASTERN":
                case "EASTERN PROVINCE":
                    return "EP";
                case "8":
                case "NWP":
                case "NORTH WESTERN":
                case "NORTH WESTERN PROVINCE":
                    return "NWP";
                case "D":
                case "NWP2":
                case "NWP 2":
                case "NORTH WESTERN 2":
                case "NORTH WESTERN PROVINCE II-EDL":
                    return "NWP 2";
                case "9":
                case "SAB":
                case "SABP":
                case "SABARAGAMUWA":
                case "SABARAGAMUWA PROVINCE":
                    return "SABP";
                case "A":
                case "NCP":
                case "NORTH CENTRAL":
                case "NORTH CENTRAL PROVINCE":
                    return "NCP";
                case "B":
                case "SP":
                case "SOUTHERN":
                case "SOUTHERN PROVINCE":
                    return "SP";
                case "F":
                case "SP2":
                case "SOUTHERN 2":
                case "SOUTHERN PROVINCE 2":
                    return "SP2";
                default:
                    return clean;
            }
        }

        public double FetchStockValue(string provinceCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode)) return 0;
            string companyId = ResolveCompanyId(provinceCode);
            double val = StockValueDao.Fetch(companyId);
            if (val == 0 && companyId == "WPS1")
            {
                // Fallback attempt for WPS
                double alt = StockValueDao.Fetch("WPS");
                if (alt > 0) return alt;
            }
            return val;
        }

        public List<PivTotalModel> FetchPivTotal(string provinceCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode)) return new List<PivTotalModel>();
            string companyId = ResolveCompanyId(provinceCode);
            var raw = PivTotalDao.Fetch(companyId) ?? new List<DgmPivTotalModel>();
            if ((raw == null || raw.Count == 0) && companyId == "WPS1")
            {
                var alt = PivTotalDao.Fetch("WPS");
                if (alt != null && alt.Count > 0) raw = alt;
            }
            return (raw ?? new List<DgmPivTotalModel>()).Select(r => new PivTotalModel
            {
                date = r.date,
                amount = r.amount
            }).OrderBy(x => x.date).ToList();
        }

        public double FetchPivPeriodSummary(string provinceCode, string startDate = null, string endDate = null)
        {
            if (string.IsNullOrWhiteSpace(provinceCode)) return 0;
            string companyId = ResolveCompanyId(provinceCode);
            double val = PivPeriodSummaryDao.Fetch(companyId, startDate, endDate);
            if (val == 0 && companyId == "WPS1")
            {
                double alt = PivPeriodSummaryDao.Fetch("WPS", startDate, endDate);
                if (alt > 0) return alt;
            }
            return val;
        }

        public List<DgmAppCountModel> FetchApplicationCounts(int year, string provinceCode, string areaCode = null)
        {
            if (string.IsNullOrWhiteSpace(provinceCode)) return new List<DgmAppCountModel>();
            string companyId = ResolveCompanyId(provinceCode);
            var raw = AppCountDao.Fetch(year, companyId) ?? new List<DgmAppCountModel>();
            if ((raw == null || raw.Count == 0) && companyId == "WPS1")
            {
                var alt = AppCountDao.Fetch(year, "WPS");
                if (alt != null && alt.Count > 0) raw = alt;
            }
            if (!string.IsNullOrWhiteSpace(areaCode) && !areaCode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var targetArea = areaCode.Trim().ToUpper();
                raw = raw.Where(x => (x.deptId ?? "").Trim().ToUpper().Contains(targetArea) || targetArea.Contains((x.deptId ?? "").Trim().ToUpper())).ToList();
            }
            return raw;
        }

        public List<DgmConnectionGivenModel> FetchConnectionsGiven(int year, string provinceCode, string areaCode = null)
        {
            if (string.IsNullOrWhiteSpace(provinceCode)) return new List<DgmConnectionGivenModel>();
            string companyId = ResolveCompanyId(provinceCode);
            var raw = ConnectionGivenDao.Fetch(year, companyId) ?? new List<DgmConnectionGivenModel>();
            if ((raw == null || raw.Count == 0) && companyId == "WPS1")
            {
                var alt = ConnectionGivenDao.Fetch(year, "WPS");
                if (alt != null && alt.Count > 0) raw = alt;
            }
            if (!string.IsNullOrWhiteSpace(areaCode) && !areaCode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var targetArea = areaCode.Trim().ToUpper();
                raw = raw.Where(x => (x.deptId ?? "").Trim().ToUpper().Contains(targetArea) || targetArea.Contains((x.deptId ?? "").Trim().ToUpper())).ToList();
            }
            return raw;
        }

        public List<DgmPendingApplicationModel> FetchPendingApplications(int year, string provinceCode, string areaCode = null)
        {
            if (string.IsNullOrWhiteSpace(provinceCode)) return new List<DgmPendingApplicationModel>();
            string companyId = ResolveCompanyId(provinceCode);
            var raw = PendingApplicationsDao.Fetch(year, companyId) ?? new List<DgmPendingApplicationModel>();
            if ((raw == null || raw.Count == 0) && companyId == "WPS1")
            {
                var alt = PendingApplicationsDao.Fetch(year, "WPS");
                if (alt != null && alt.Count > 0) raw = alt;
            }
            if (!string.IsNullOrWhiteSpace(areaCode) && !areaCode.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var targetArea = areaCode.Trim().ToUpper();
                raw = raw.Where(x => (x.deptId ?? "").Trim().ToUpper().Contains(targetArea) || targetArea.Contains((x.deptId ?? "").Trim().ToUpper())).ToList();
            }
            return raw;
        }

        public IntegratedConstructionSummaryModel FetchSummary(string provinceCode, int year, string startDate = null, string endDate = null, string areaCode = null)
        {
            var pCode = string.IsNullOrWhiteSpace(provinceCode) ? "WPN" : provinceCode.Trim();
            var targetYear = year <= 0 ? DateTime.Today.Year : year;

            // Run all 6 DB queries in parallel for maximum throughput
            double stock = 0;
            List<PivTotalModel> pivList = null;
            double pivPeriod = 0;
            List<DgmAppCountModel> apps = null;
            List<DgmConnectionGivenModel> conns = null;
            List<DgmPendingApplicationModel> pendings = null;

            var tasks = new[]
            {
                Task.Run(() => { stock = FetchStockValue(pCode); }),
                Task.Run(() => { pivList = FetchPivTotal(pCode); }),
                Task.Run(() => { pivPeriod = FetchPivPeriodSummary(pCode, startDate, endDate); }),
                Task.Run(() => { apps = FetchApplicationCounts(targetYear, pCode, areaCode); }),
                Task.Run(() => { conns = FetchConnectionsGiven(targetYear, pCode, areaCode); }),
                Task.Run(() => { pendings = FetchPendingApplications(targetYear, pCode, areaCode); }),
            };

            Task.WaitAll(tasks);

            return new IntegratedConstructionSummaryModel
            {
                ProvinceCode = ResolveCompanyId(pCode),
                Year = targetYear,
                StockValue = stock,
                PivTotal = pivList ?? new List<PivTotalModel>(),
                PivPeriodSummary = pivPeriod,
                ApplicationCounts = apps ?? new List<DgmAppCountModel>(),
                ConnectionsGiven = conns ?? new List<DgmConnectionGivenModel>(),
                PendingApplications = pendings ?? new List<DgmPendingApplicationModel>(),
                FetchedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
