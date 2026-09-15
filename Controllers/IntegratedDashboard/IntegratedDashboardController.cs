using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Web.Http;
using MISReports_Api.DAL.IntegratedDashboard;
using MISReports_Api.Models.FinancialDashboard;
using MISReports_Api.Models.IntegratedDashboard;

namespace MISReports_Api.Controllers.IntegratedDashboard
{
    [RoutePrefix("api/integrated")]
    public class IntegratedDashboardController : ApiController
    {
        private static readonly IntegratedFinancialDao Dao = new IntegratedFinancialDao();
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
                Trace.TraceInformation($"[IntegratedDashboard] {label} took {sw.ElapsedMilliseconds} ms");
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
                        Trace.TraceError($"[IntegratedDashboard] {key}-refresh failed: {ex.Message}");
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
        [Route("financial/summary")]
        public IHttpActionResult GetFinancialSummary(string division = "all", string province = "all", string area = "all", bool refresh = false)
        {
            string cacheKey = $"integrated-fin-summary-{division ?? "all"}-{province ?? "all"}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => Dao.FetchSummary(division, province, area)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("financial/piv-total")]
        public IHttpActionResult GetPivTotal(string division = "all", bool refresh = false)
        {
            string cacheKey = $"integrated-piv-total-{division ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => Dao.FetchPivTotal(division)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("financial/piv-division")]
        public IHttpActionResult GetPivDivision(string division = "all", bool refresh = false)
        {
            string cacheKey = $"integrated-piv-division-{division ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => Dao.FetchPivDivision(division)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("financial/stock-total")]
        public IHttpActionResult GetStockTotal(string division = "all", bool refresh = false)
        {
            string cacheKey = $"integrated-stock-total-{division ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => Dao.FetchStockTotal(division)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("financial/stock-division")]
        public IHttpActionResult GetStockDivision(string division = "all", bool refresh = false)
        {
            string cacheKey = $"integrated-stock-division-{division ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => Dao.FetchStockDivision(division)));

            return Ok(meta);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Construction Progress Endpoints (for selected province)
        // ══════════════════════════════════════════════════════════════════════
        private static readonly IntegratedConstructionDao ConstructionDao = new IntegratedConstructionDao();

        [HttpGet]
        [Route("construction/summary")]
        public IHttpActionResult GetConstructionSummary(string province = "WPN", int year = 2026, string startDate = null, string endDate = null, string area = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim().ToUpper();
            string cacheKey = $"integrated-const-summary-{pCode}-{year}-{startDate ?? ""}-{endDate ?? ""}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => ConstructionDao.FetchSummary(pCode, year, startDate, endDate, area)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("construction/stock-value")]
        public IHttpActionResult GetConstructionStockValue(string province = "WPN", bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim().ToUpper();
            string cacheKey = $"integrated-const-stock-{pCode}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => new { stockValue = ConstructionDao.FetchStockValue(pCode) }));

            return Ok(meta);
        }

        [HttpGet]
        [Route("construction/piv-total")]
        public IHttpActionResult GetConstructionPivTotal(string province = "WPN", bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim().ToUpper();
            string cacheKey = $"integrated-const-piv-{pCode}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => ConstructionDao.FetchPivTotal(pCode)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("construction/piv-period-summary")]
        public IHttpActionResult GetConstructionPivPeriodSummary(string province = "WPN", string startDate = null, string endDate = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim().ToUpper();
            string cacheKey = $"integrated-const-piv-period-{pCode}-{startDate ?? ""}-{endDate ?? ""}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => new { pivCollection = ConstructionDao.FetchPivPeriodSummary(pCode, startDate, endDate) }));

            return Ok(meta);
        }

        [HttpGet]
        [Route("construction/application-count")]
        public IHttpActionResult GetConstructionApplicationCount(string province = "WPN", int year = 2026, string area = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim().ToUpper();
            string cacheKey = $"integrated-const-apps-{pCode}-{year}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => ConstructionDao.FetchApplicationCounts(year, pCode, area)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("construction/connections-given")]
        public IHttpActionResult GetConstructionConnectionsGiven(string province = "WPN", int year = 2026, string area = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim().ToUpper();
            string cacheKey = $"integrated-const-conns-{pCode}-{year}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => ConstructionDao.FetchConnectionsGiven(year, pCode, area)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("construction/pending-applications")]
        public IHttpActionResult GetConstructionPendingApplications(string province = "WPN", int year = 2026, string area = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim().ToUpper();
            string cacheKey = $"integrated-const-pendings-{pCode}-{year}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => ConstructionDao.FetchPendingApplications(year, pCode, area)));

            return Ok(meta);
        }

        // =========================================================================
        // INTEGRATED DASHBOARD - AREA ENGINEER DASHBOARD INTEGRATION
        // =========================================================================

        private static readonly IntegratedAreaEngineerDao AreaEngineerDao = new IntegratedAreaEngineerDao();

        [HttpGet]
        [Route("areaengineer/summary")]
        public IHttpActionResult GetAreaEngineerSummary(string province = "WPN", int year = 2026, string startDate = null, string endDate = null, string area = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim();
            string cacheKey = $"integrated-ae-summary-{pCode}-{year}-{startDate ?? ""}-{endDate ?? ""}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerDao.FetchSummary(pCode, year, startDate, endDate, area)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("areaengineer/stock-value")]
        public IHttpActionResult GetAreaEngineerStockValue(string province = "WPN", bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim();
            string cacheKey = $"integrated-ae-stock-value-{pCode}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => new { stockValue = AreaEngineerDao.FetchStockValue(pCode) }));

            return Ok(meta);
        }

        [HttpGet]
        [Route("areaengineer/piv-total")]
        public IHttpActionResult GetAreaEngineerPivTotal(string province = "WPN", bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim();
            string cacheKey = $"integrated-ae-piv-total-{pCode}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerDao.FetchPivTotal(pCode)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("areaengineer/piv-period-summary")]
        public IHttpActionResult GetAreaEngineerPivPeriodSummary(string province = "WPN", string startDate = null, string endDate = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim();
            string cacheKey = $"integrated-ae-piv-period-{pCode}-{startDate ?? ""}-{endDate ?? ""}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => new { pivCollection = AreaEngineerDao.FetchPivPeriodSummary(pCode, startDate, endDate) }));

            return Ok(meta);
        }

        [HttpGet]
        [Route("areaengineer/material-master")]
        public IHttpActionResult GetAreaEngineerMaterialMaster(string province = "WPN", bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim();
            string cacheKey = $"integrated-ae-material-master-{pCode}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerDao.FetchMaterialMaster(pCode)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("areaengineer/application-count")]
        public IHttpActionResult GetAreaEngineerApplicationCount(string province = "WPN", int year = 2026, string area = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim();
            string cacheKey = $"integrated-ae-app-count-{pCode}-{year}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerDao.FetchApplicationCounts(year, pCode, area)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("areaengineer/connections-given")]
        public IHttpActionResult GetAreaEngineerConnectionsGiven(string province = "WPN", int year = 2026, string area = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim();
            string cacheKey = $"integrated-ae-conns-{pCode}-{year}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerDao.FetchConnectionsGiven(year, pCode, area)));

            return Ok(meta);
        }

        [HttpGet]
        [Route("areaengineer/pending-applications")]
        public IHttpActionResult GetAreaEngineerPendingApplications(string province = "WPN", int year = 2026, string area = null, bool refresh = false)
        {
            string pCode = string.IsNullOrWhiteSpace(province) ? "WPN" : province.Trim();
            string cacheKey = $"integrated-ae-pendings-{pCode}-{year}-{area ?? "all"}";
            if (refresh)
            {
                Cache.TryRemove(cacheKey, out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata(cacheKey, () =>
                ExecuteWithTiming(cacheKey, () => AreaEngineerDao.FetchPendingApplications(year, pCode, area)));

            return Ok(meta);
        }
    }
}

