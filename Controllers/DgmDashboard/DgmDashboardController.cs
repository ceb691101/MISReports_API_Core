using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Web.Http;
using MISReports_Api.DAL.DgmDashboard;
using MISReports_Api.Models.DgmDashboard;
using MISReports_Api.Models.FinancialDashboard; // for CachedValue<T>

namespace MISReports_Api.Controllers.DgmDashboard
{
    [RoutePrefix("api/dgm")]
    public class DgmDashboardController : ApiController
    {
        private static readonly DgmPivTotalDao DgmPivTotalDao = new DgmPivTotalDao();
        private static readonly DgmPivPeriodSummaryDao DgmPivPeriodSummaryDao = new DgmPivPeriodSummaryDao();
        private static readonly DgmStockValueDao DgmStockValueDao = new DgmStockValueDao();
        private static readonly DgmAppCountDao DgmAppCountDao = new DgmAppCountDao();
        private static readonly DgmConnectionGivenDao DgmConnectionGivenDao = new DgmConnectionGivenDao();
        private static readonly DgmPendingApplicationsDao DgmPendingApplicationsDao = new DgmPendingApplicationsDao();
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
        public IHttpActionResult GetPivTotal(string companyId = "WPN", bool refresh = false)
        {
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"dgm-piv-total-{targetCompany}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => DgmPivTotalDao.Fetch(targetCompany)));
            return Ok(meta);
        }

        [HttpGet]
        [Route("piv-period-summary")]
        public IHttpActionResult GetPivPeriodSummary(string companyId = "WPN", string startDate = null, string endDate = null, bool refresh = false)
        {
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"dgm-piv-period-summary-{targetCompany}-{startDate ?? ""}-{endDate ?? ""}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => new DgmPivPeriodSummaryModel
                {
                    pivCollection = DgmPivPeriodSummaryDao.Fetch(targetCompany, startDate, endDate)
                }));
            return Ok(meta);
        }

        [HttpGet]
        [Route("stock-value")]
        public IHttpActionResult GetStockValue(string companyId = "WPN", bool refresh = false)
        {
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"dgm-stock-value-{targetCompany}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => new DgmStockValueModel
                {
                    stockValue = DgmStockValueDao.Fetch(targetCompany)
                }));
            return Ok(meta);
        }

        [HttpGet]
        [Route("application-count")]
        public IHttpActionResult GetApplicationCount(string companyId = "WPN", int? year = null, bool refresh = false)
        {
            int targetYear = year ?? DateTime.Today.Year;
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"dgm-application-count-{targetCompany}-{targetYear}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => DgmAppCountDao.Fetch(targetYear, targetCompany)));
            return Ok(meta);
        }

        [HttpGet]
        [Route("connections-given")]
        public IHttpActionResult GetConnectionsGiven(string companyId = "WPN", int? year = null, bool refresh = false)
        {
            int targetYear = year ?? DateTime.Today.Year;
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"dgm-connections-given-{targetCompany}-{targetYear}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => DgmConnectionGivenDao.Fetch(targetYear, targetCompany)));
            return Ok(meta);
        }

        [HttpGet]
        [Route("pending-applications")]
        public IHttpActionResult GetPendingApplications(string companyId = "WPN", int? year = null, bool refresh = false)
        {
            int targetYear = year ?? DateTime.Today.Year;
            string targetCompany = string.IsNullOrWhiteSpace(companyId) ? "WPN" : companyId.Trim().ToUpper();
            string cacheKey = $"dgm-pending-applications-{targetCompany}-{targetYear}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => DgmPendingApplicationsDao.Fetch(targetYear, targetCompany)));
            return Ok(meta);
        }
    }
}
