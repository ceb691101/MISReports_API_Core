using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Http;
using MISReports_Api.DAL.FinancialDashboard;
using MISReports_Api.Models.FinancialDashboard;

namespace MISReports_Api.Controllers
{
    public class FinancialDashboardController : ApiController
    {
        private static readonly PivTotalDao PivTotalDao = new PivTotalDao();
        private static readonly PivDivisionDao PivDivisionDao = new PivDivisionDao();
        private static readonly StockTotalDao StockTotalDao = new StockTotalDao();
        private static readonly StockDivisionDao StockDivisionDao = new StockDivisionDao();

        private static readonly ConcurrentDictionary<string, object> Cache = new ConcurrentDictionary<string, object>();
        private const double CacheMinutes = 5;
        private static readonly object RefreshLock = new object();
        private static bool IsRefreshing;
        private static System.Timers.Timer WarmTimer;

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

        private static void EnsureWarmTimer()
        {
            if (WarmTimer != null)
            {
                return;
            }

            lock (RefreshLock)
            {
                if (WarmTimer != null)
                {
                    return;
                }

                WarmTimer = new System.Timers.Timer(TimeSpan.FromMinutes(CacheMinutes).TotalMilliseconds)
                {
                    AutoReset = true,
                };
                WarmTimer.Elapsed += (s, e) => { _ = RefreshAllAsync(); };
                WarmTimer.Start();
                _ = RefreshAllAsync();
            }
        }

        private static async Task RefreshAllAsync()
        {
            if (IsRefreshing)
            {
                return;
            }

            lock (RefreshLock)
            {
                if (IsRefreshing)
                {
                    return;
                }

                IsRefreshing = true;
            }

            try
            {
                var totalTask = Task.Run(() => ExecuteWithTiming("piv-total", PivTotalDao.Fetch));
                var divTask = Task.Run(() => ExecuteWithTiming("piv-division", PivDivisionDao.Fetch));
                var stockTotalTask = Task.Run(() => ExecuteWithTiming("stock-total", StockTotalDao.Fetch));
                var stockDivTask = Task.Run(() => ExecuteWithTiming("stock-division", StockDivisionDao.Fetch));

                await Task.WhenAll(totalTask, divTask, stockTotalTask, stockDivTask);

                SetCache("piv-total", totalTask.Result);
                SetCache("piv-division", divTask.Result);
                SetCache("stock-total", stockTotalTask.Result);
                SetCache("stock-division", stockDivTask.Result);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"warm-refresh failed: {ex.Message}");
            }
            finally
            {
                lock (RefreshLock)
                {
                    IsRefreshing = false;
                }
            }
        }

        public FinancialDashboardController()
        {
            EnsureWarmTimer();
        }

        [HttpGet]
        [Route("api/piv/piv-total")]
        public IHttpActionResult GetPivTotal(bool refresh = false)
        {
            if (refresh)
            {
                Cache.TryRemove("piv-total", out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata("piv-total", () =>
                ExecuteWithTiming("piv-total", PivTotalDao.Fetch));
            return Ok(meta);
        }

        [HttpGet]
        [Route("api/piv/piv-division")]
        public IHttpActionResult GetPivDivision(bool refresh = false)
        {
            if (refresh)
            {
                Cache.TryRemove("piv-division", out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata("piv-division", () =>
                ExecuteWithTiming("piv-division", PivDivisionDao.Fetch));
            return Ok(meta);
        }

        [HttpGet]
        [Route("api/piv/stock-total")]
        public IHttpActionResult GetStockTotal(bool refresh = false)
        {
            if (refresh)
            {
                Cache.TryRemove("stock-total", out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata("stock-total", () =>
                ExecuteWithTiming("stock-total", StockTotalDao.Fetch));
            return Ok(meta);
        }

        [HttpGet]
        [Route("api/piv/stock-division")]
        public IHttpActionResult GetStockDivision(bool refresh = false)
        {
            if (refresh)
            {
                Cache.TryRemove("stock-division", out _);
            }

            var meta = GetOrReturnStaleAndRefreshWithMetadata("stock-division", () =>
                ExecuteWithTiming("stock-division", StockDivisionDao.Fetch));
            return Ok(meta);
        }
    }
}
