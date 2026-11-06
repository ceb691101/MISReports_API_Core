using MISReports_Api.Compat;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MISReports_Api.DAL.AreaEngineerDashboard;
using MISReports_Api.Models.AreaEngineerDashboard;
using MISReports_Api.Models.DgmDashboard;
using MISReports_Api.Models.FinancialDashboard;

namespace MISReports_Api.Controllers.AreaEngineerDashboard
{
    [RoutePrefix("api/areaengineer")]
    public class AreaEngineerDashboardController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly AreaEngineerPivTotalDAL AreaEngineerPivTotalDAL = new AreaEngineerPivTotalDAL();
        private static readonly AreaEngineerPivPeriodSummaryDAL AreaEngineerPivPeriodSummaryDAL = new AreaEngineerPivPeriodSummaryDAL();
        private static readonly AreaEngineerStockValueDAL AreaEngineerStockValueDAL = new AreaEngineerStockValueDAL();
        private static readonly AreaEngineerAppCountDAL AreaEngineerAppCountDAL = new AreaEngineerAppCountDAL();
        private static readonly AreaEngineerConnectionGivenDAL AreaEngineerConnectionGivenDAL = new AreaEngineerConnectionGivenDAL();
        private static readonly AreaEngineerPendingApplicationsDAL AreaEngineerPendingApplicationsDAL = new AreaEngineerPendingApplicationsDAL();
        private static readonly AreaEngineerMaterialMasterDAL AreaEngineerMaterialMasterDAL = new AreaEngineerMaterialMasterDAL();

        private static readonly ConcurrentDictionary<string, object> Cache = new ConcurrentDictionary<string, object>();
        private const double CacheMinutes = 5;

        private static void SetCache<T>(string key, T data)
        {
            Cache[key] = new CachedValue<T>
            {
                Value = data,
                FetchedAt = DateTimeOffset.UtcNow
            };
        }

        private static T ExecuteWithTiming<T>(string label, Func<T> work)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return work();
            }
            finally
            {
                sw.Stop();
                Trace.TraceInformation($"{label} took {sw.ElapsedMilliseconds} ms");
            }
        }

        private static CachedValue<T> GetOrReturnStaleAndRefreshWithMetadata<T>(string key, Func<T> factory)
        {
            Cache.TryGetValue(key, out var cacheObj);
            var cached = cacheObj as CachedValue<T>;
            var now = DateTimeOffset.UtcNow;
            var freshWindow = TimeSpan.FromMinutes(CacheMinutes);

            if (cached != null)
            {
                if (now - cached.FetchedAt < freshWindow)
                {
                    return cached;
                }

                _ = Task.Run(() =>
                {
                    try
                    {
                        var data = ExecuteWithTiming(key + "-refresh", factory);
                        SetCache(key, data);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "GetOrReturnStaleAndRefreshWithMetadata failed");
                        Trace.TraceError($"{key}-refresh failed: {ex.Message}");
                    }
                });

                return cached;
            }

            var freshData = ExecuteWithTiming(key + "-miss", factory);
            var result = new CachedValue<T>
            {
                Value = freshData,
                FetchedAt = DateTimeOffset.UtcNow
            };
            SetCache(key, freshData);
            return result;
        }

        [HttpGet]
        [Route("piv-total")]
        public IActionResult GetPivTotal(string companyId = "WPN", bool refresh = false)
        {
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"areaengineer-piv-total-{targetCompany}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerPivTotalDAL.Fetch(targetCompany)));
            return Ok(meta);
        }

        [HttpGet]
        [Route("piv-period-summary")]
        public IActionResult GetPivPeriodSummary(string companyId = "WPN", string startDate = null, string endDate = null, bool refresh = false)
        {
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"areaengineer-piv-period-summary-{targetCompany}-{startDate ?? ""}-{endDate ?? ""}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => new DgmPivPeriodSummaryModel
                {
                    pivCollection = AreaEngineerPivPeriodSummaryDAL.Fetch(targetCompany, startDate, endDate)
                }));
            return Ok(meta);
        }

        [HttpGet]
        [Route("stock-value")]
        public IActionResult GetStockValue(string companyId = "WPN", bool refresh = false)
        {
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"areaengineer-stock-value-{targetCompany}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => new DgmStockValueModel
                {
                    stockValue = AreaEngineerStockValueDAL.Fetch(targetCompany)
                }));
            return Ok(meta);
        }

        [HttpGet]
        [Route("application-count")]
        public IActionResult GetApplicationCount(string companyId = "WPN", int? year = null, bool refresh = false)
        {
            int targetYear = year ?? DateTime.Today.Year;
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"areaengineer-application-count-{targetCompany}-{targetYear}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerAppCountDAL.Fetch(targetYear, targetCompany)));
            return Ok(meta);
        }

        [HttpGet]
        [Route("connections-given")]
        public IActionResult GetConnectionsGiven(string companyId = "WPN", int? year = null, bool refresh = false)
        {
            int targetYear = year ?? DateTime.Today.Year;
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"areaengineer-connections-given-{targetCompany}-{targetYear}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerConnectionGivenDAL.Fetch(targetYear, targetCompany)));
            return Ok(meta);
        }

        [HttpGet]
        [Route("pending-applications")]
        public IActionResult GetPendingApplications(string companyId = "WPN", int? year = null, bool refresh = false)
        {
            int targetYear = year ?? DateTime.Today.Year;
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"areaengineer-pending-applications-{targetCompany}-{targetYear}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerPendingApplicationsDAL.Fetch(targetYear, targetCompany)));
            return Ok(meta);
        }

        [HttpGet]
        [Route("material-master")]
        public IActionResult GetMaterialMaster(string companyId = "WPN", bool refresh = false)
        {
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"areaengineer-material-master-{targetCompany}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerMaterialMasterDAL.Fetch(targetCompany)));
            return Ok(meta);
        }
    }
}
