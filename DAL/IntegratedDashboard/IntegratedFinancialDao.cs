using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MISReports_Api.DAL.FinancialDashboard;
using MISReports_Api.Models.FinancialDashboard;
using MISReports_Api.Models.IntegratedDashboard;

namespace MISReports_Api.DAL.IntegratedDashboard
{
    public class IntegratedFinancialDao
    {
        private readonly PivTotalDao _pivTotalDao;
        private readonly PivDivisionDao _pivDivisionDao;
        private readonly StockTotalDao _stockTotalDao;
        private readonly StockDivisionDao _stockDivisionDao;

        public IntegratedFinancialDao()
        {
            _pivTotalDao = new PivTotalDao();
            _pivDivisionDao = new PivDivisionDao();
            _stockTotalDao = new StockTotalDao();
            _stockDivisionDao = new StockDivisionDao();
        }

        public string NormalizeCompany(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Other";
            var v = value.Trim();
            return v.Equals("A", StringComparison.OrdinalIgnoreCase) ? "hq" : v;
        }

        public string NormalizeDivision(string division)
        {
            if (string.IsNullOrWhiteSpace(division) || division.Equals("all", StringComparison.OrdinalIgnoreCase))
                return "all";

            var trimmed = division.Trim().ToLowerInvariant();
            if (trimmed.StartsWith("disco", StringComparison.OrdinalIgnoreCase) && trimmed.Length >= 6)
            {
                return $"d{trimmed.Substring(5, 1)}";
            }
            if (trimmed.StartsWith("r", StringComparison.OrdinalIgnoreCase) && trimmed.Length >= 2)
            {
                return $"d{trimmed.Substring(1, 1)}";
            }
            return trimmed;
        }

        public List<PivDivisionModel> FetchPivDivision(string division = null)
        {
            var raw = _pivDivisionDao.Fetch() ?? new List<PivDivisionModel>();
            var targetDiv = NormalizeDivision(division);

            if (targetDiv == "all")
            {
                return raw;
            }

            return raw.Where(item =>
            {
                var comp = NormalizeCompany(item.company).ToLowerInvariant();
                return comp == targetDiv;
            }).ToList();
        }

        public List<PivTotalModel> FetchPivTotal(string division = null)
        {
            var targetDiv = NormalizeDivision(division);
            if (targetDiv == "all")
            {
                return _pivTotalDao.Fetch() ?? new List<PivTotalModel>();
            }

            // If filtered by division, calculate daily sums from filtered division records
            var divRecords = FetchPivDivision(targetDiv);
            var dailyMap = new Dictionary<string, double>();

            foreach (var item in divRecords)
            {
                if (!string.IsNullOrEmpty(item.date))
                {
                    if (dailyMap.ContainsKey(item.date))
                        dailyMap[item.date] += item.amount;
                    else
                        dailyMap[item.date] = item.amount;
                }
            }

            return dailyMap.Select(kvp => new PivTotalModel
            {
                date = kvp.Key,
                amount = kvp.Value
            }).OrderByDescending(x => x.date).ToList();
        }

        public List<StockDivisionModel> FetchStockDivision(string division = null)
        {
            var raw = _stockDivisionDao.Fetch() ?? new List<StockDivisionModel>();
            var targetDiv = NormalizeDivision(division);

            if (targetDiv == "all")
            {
                return raw;
            }

            return raw.Where(item =>
            {
                var comp = NormalizeCompany(item.company).ToLowerInvariant();
                return comp == targetDiv;
            }).ToList();
        }

        public double FetchStockTotal(string division = null)
        {
            var targetDiv = NormalizeDivision(division);
            if (targetDiv == "all")
            {
                return _stockTotalDao.Fetch();
            }

            var divRecords = FetchStockDivision(targetDiv);
            return divRecords.Sum(item => item.amount);
        }

        public IntegratedFinancialSummaryModel FetchSummary(string division = null, string province = null, string area = null)
        {
            var targetDiv = NormalizeDivision(division);

            List<PivDivisionModel> rawPivDiv = null;
            List<StockDivisionModel> rawStockDiv = null;

            // Run PIV Division and Stock Division queries concurrently
            var tasks = new[]
            {
                Task.Run(() => { rawPivDiv = _pivDivisionDao.Fetch() ?? new List<PivDivisionModel>(); }),
                Task.Run(() => { rawStockDiv = _stockDivisionDao.Fetch() ?? new List<StockDivisionModel>(); })
            };

            Task.WaitAll(tasks);

            // Filter division dataset in-memory
            var divList = targetDiv == "all"
                ? (rawPivDiv ?? new List<PivDivisionModel>())
                : (rawPivDiv ?? new List<PivDivisionModel>()).Where(item =>
                {
                    var comp = NormalizeCompany(item.company).ToLowerInvariant();
                    return comp == targetDiv;
                }).ToList();

            var stockDivList = targetDiv == "all"
                ? (rawStockDiv ?? new List<StockDivisionModel>())
                : (rawStockDiv ?? new List<StockDivisionModel>()).Where(item =>
                {
                    var comp = NormalizeCompany(item.company).ToLowerInvariant();
                    return comp == targetDiv;
                }).ToList();

            // Calculate PivTotal (daily sums) directly in-memory from divList
            // Mathematically identical to _pivTotalDao but saves a redundant 5.3s database query
            var dailyMap = new Dictionary<string, double>();
            foreach (var item in divList)
            {
                if (!string.IsNullOrEmpty(item.date))
                {
                    if (dailyMap.ContainsKey(item.date))
                        dailyMap[item.date] += item.amount;
                    else
                        dailyMap[item.date] = item.amount;
                }
            }

            var totalList = dailyMap.Select(kvp => new PivTotalModel
            {
                date = kvp.Key,
                amount = kvp.Value
            }).OrderByDescending(x => x.date).ToList();

            // Calculate StockTotal directly in-memory from stockDivList
            double stockTotalVal = stockDivList.Sum(item => item.amount);

            var total7Day = totalList.Sum(x => x.amount);
            var latestDate = totalList.OrderByDescending(x => x.date).FirstOrDefault()?.date ?? string.Empty;
            var latestTotal = totalList.FirstOrDefault(x => x.date == latestDate)?.amount ?? 0;

            return new IntegratedFinancialSummaryModel
            {
                PivTotal = totalList,
                PivDivision = divList,
                StockTotal = stockTotalVal,
                StockDivision = stockDivList,
                Total7DayCollection = total7Day,
                LatestPivDate = latestDate,
                LatestPivTotal = latestTotal,
                FetchedAt = DateTimeOffset.UtcNow,
                AppliedDivision = division ?? "all",
                AppliedProvince = province ?? "all",
                AppliedArea = area ?? "all"
            };
        }
    }
}
