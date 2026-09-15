using MISReports_Api.DAL.Dashboard;
using MISReports_Api.Models.Dashboard;
using System;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    // =========================================================================
    // 1. DEFAULT DASHBOARD CONTROLLER
    //    Routes: api/dashboard/...
    // =========================================================================
    [RoutePrefix("api/dashboard")]
    public class DashboardController : ApiController
    {
        private readonly BulkCustomersDao _bulkCustomersDao = new BulkCustomersDao();
        private readonly SalesAndCollectionRangeDao _salesAndCollectionRangeDao = new SalesAndCollectionRangeDao();
        private readonly OrdinaryCustomersDao _ordinaryCustomersDao = new OrdinaryCustomersDao();
        private readonly KioskCollectionDao _kioskCollectionDao = new KioskCollectionDao();
        private readonly TopCustomersDao _topCustomersDao = new TopCustomersDao();

        /// <summary>GET api/dashboard/customers/active-count</summary>
        [HttpGet]
        [Route("customers/active-count")]
        public IHttpActionResult GetActiveCustomerCount([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            try
            {
                if (!_bulkCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                int count = _bulkCustomersDao.GetActiveCustomerCount(NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data = new { activeCustomerCount = count },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get active customer count.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/ordinary-customers-summary?region={region}</summary>
        [HttpGet]
        [Route("ordinary-customers-summary")]
        public IHttpActionResult GetOrdinaryCustomersSummary([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            try
            {
                if (!_ordinaryCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = _ordinaryCustomersDao.GetOrdinaryCustomersCount(NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new { data, errorMessage = (string)null });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving ordinary customers summary.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/salesCollection/range/ordinary</summary>
        [HttpGet]
        [Route("salesCollection/range/ordinary")]
        public IHttpActionResult GetOrdinarySalesAndCollection([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            try
            {
                if (!_salesAndCollectionRangeDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                SalesAndCollectionRangeResult result = _salesAndCollectionRangeDao.GetSalesAndCollectionRange(NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data = new
                    {
                        records = result.OrdinaryData.Select(rec => new
                        {
                            Date = rec.Date,
                            Amount = rec.Collection,
                            rec.ErrorMessage
                        })
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get ordinary sales and collection data.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/salesCollection/range/bulk</summary>
        [HttpGet]
        [Route("salesCollection/range/bulk")]
        public IHttpActionResult GetBulkSalesAndCollection([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            try
            {
                if (!_salesAndCollectionRangeDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                SalesAndCollectionRangeResult result = _salesAndCollectionRangeDao.GetSalesAndCollectionRange(NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data = new
                    {
                        records = result.BulkData.Select(rec => new
                        {
                            Date = rec.Date,
                            Amount = rec.Collection,
                            rec.ErrorMessage
                        })
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get bulk sales and collection data.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/kiosk-collection?userId=KIOS00</summary>
        [HttpGet]
        [Route("kiosk-collection")]
        public IHttpActionResult GetKioskCollection([FromUri] string userId = null, [FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            try
            {
                if (!_kioskCollectionDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                if (string.IsNullOrWhiteSpace(userId))
                    return Ok(new { data = (object)null, errorMessage = "userId is required." });

                string resolvedUserId = userId.Trim();

                // Fixed range for kiosk: last 7 days ending yesterday.
                DateTime resolvedToDate = DateTime.Today.AddDays(-1);
                DateTime resolvedFromDate = resolvedToDate.AddDays(-6);

                var records = _kioskCollectionDao.GetKioskCollection(resolvedUserId, NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data = new
                    {
                        userId = resolvedUserId,
                        fromDate = resolvedFromDate.ToString("dd-MM-yy"),
                        toDate = resolvedToDate.ToString("dd-MM-yy"),
                        records
                    },
                    errorMessage = (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get kiosk collection data.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/top-customers/billcycle/max</summary>
        [HttpGet]
        [Route("top-customers/billcycle/max")]
        public IHttpActionResult GetTopCustomersMaxBillCycle()
        {
            try
            {
                if (!_topCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var maxBillCycle = _topCustomersDao.GetLatestBillCycle();

                return Ok(new
                {
                    data = new { billCycle = maxBillCycle },
                    errorMessage = string.IsNullOrWhiteSpace(maxBillCycle) ? "No bill cycle found in mon_tot." : (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving max bill cycle.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/top-customers/list (fetches latest bill cycle and top customers with optional region filter)</summary>
        [HttpGet]
        [Route("top-customers/list")]
        public IHttpActionResult GetTopCustomers([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null, [FromUri] int take = 10)
        {
            try
            {
                if (!_topCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                // Fetch the max bill cycle and return top customers with region filter
                var data = _topCustomersDao.GetTopCustomers(null, NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area), take);

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Cannot get top customers data.", errorDetails = ex.Message });
            }
        }

        private static string NormalizeBillCycle(string billCycle)
        {
            if (string.IsNullOrWhiteSpace(billCycle))
                return null;

            var normalized = billCycle.Trim();

            if ((normalized.StartsWith("{") && normalized.EndsWith("}")) ||
                normalized.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                return null;

            return normalized;
        }

        private static string NormalizeRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return null;

            string normalized = region.Trim().ToUpperInvariant();
            if (normalized == "ALL")
                return null;

            return normalized;
        }

        private static string NormalizeProvince(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return null;

            var normalized = province.Trim().ToUpperInvariant();
            return normalized == "ALL" ? null : normalized;
        }

        private static string NormalizeArea(string area)
        {
            if (string.IsNullOrWhiteSpace(area))
                return null;

            var normalized = area.Trim().ToUpperInvariant();
            return normalized == "ALL" ? null : normalized;
        }
    }


    // =========================================================================
    // 2. SOLAR ORDINARY CUSTOMERS CONTROLLER
    //    Routes: api/dashboard/solar-ordinary-customers/...
    // =========================================================================
    [RoutePrefix("api/dashboard/solar-ordinary-customers")]
    public class SolarOrdinaryCustomersController : ApiController
    {
        private readonly SolarOrdinaryCustomersDao _solarOrdinaryCustomersDao = new SolarOrdinaryCustomersDao();

        /// <summary>GET api/dashboard/solar-ordinary-customers/billcycle/max</summary>
        [HttpGet]
        [Route("billcycle/max")]
        public IHttpActionResult GetMaxBillCycle()
        {
            try
            {
                if (!_solarOrdinaryCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var maxBillCycle = _solarOrdinaryCustomersDao.GetLatestBillCycle();

                return Ok(new
                {
                    data = new { billCycle = maxBillCycle },
                    errorMessage = string.IsNullOrWhiteSpace(maxBillCycle) ? "No bill cycle found in netprogrs." : (string)null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving max bill cycle.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count</summary>
        [HttpGet]
        [Route("count")]
        public IHttpActionResult GetTotalCustomersCount([FromUri] string billCycle = null, [FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCustomersCountResponse(billCycle, region, province, area, _solarOrdinaryCustomersDao.GetTotalCustomersCount, "Error retrieving total customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count/net-type-1</summary>
        [HttpGet]
        [Route("count/net-type-1")]
        public IHttpActionResult GetNetType1CustomersCount([FromUri] string billCycle = null, [FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCustomersCountResponse(billCycle, region, province, area, _solarOrdinaryCustomersDao.GetNetMeteringCustomersCount, "Error retrieving net type 1 customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count/net-type-2</summary>
        [HttpGet]
        [Route("count/net-type-2")]
        public IHttpActionResult GetNetType2CustomersCount([FromUri] string billCycle = null, [FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCustomersCountResponse(billCycle, region, province, area, _solarOrdinaryCustomersDao.GetNetAccountingCustomersCount, "Error retrieving net type 2 customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count/net-type-3</summary>
        [HttpGet]
        [Route("count/net-type-3")]
        public IHttpActionResult GetNetType3CustomersCount([FromUri] string billCycle = null, [FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCustomersCountResponse(billCycle, region, province, area, _solarOrdinaryCustomersDao.GetNetPlusCustomersCount, "Error retrieving net type 3 customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/count/net-type-4</summary>
        [HttpGet]
        [Route("count/net-type-4")]
        public IHttpActionResult GetNetType4CustomersCount([FromUri] string billCycle = null, [FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCustomersCountResponse(billCycle, region, province, area, _solarOrdinaryCustomersDao.GetNetPlusPlusCustomersCount, "Error retrieving net type 4 customers count.");
        }

        /// <summary>GET api/dashboard/solar-ordinary-customers/generation-capacity?billCycle=401&cycles=12</summary>
        [HttpGet]
        [Route("generation-capacity")]
        public IHttpActionResult GetGenerationCapacityGraph([FromUri] string billCycle = null, [FromUri] int cycles = 12, [FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            try
            {
                if (!_solarOrdinaryCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = _solarOrdinaryCustomersDao.GetGenerationCapacityGraph(NormalizeBillCycle(billCycle), cycles, NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving solar ordinary generation capacity graph.", errorDetails = ex.Message });
            }
        }

        // ── Shared helper ─────────────────────────────────────────────────────
        private IHttpActionResult GetCustomersCountResponse(
            string billCycle,
            string region,
            string province,
            string area,
            Func<string, string, string, string, SolarOrdinaryCustomersCount> countGetter,
            string fallbackErrorMessage)
        {
            try
            {
                if (!_solarOrdinaryCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = countGetter(NormalizeBillCycle(billCycle), NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = fallbackErrorMessage, errorDetails = ex.Message });
            }
        }

        private static string NormalizeBillCycle(string billCycle)
        {
            if (string.IsNullOrWhiteSpace(billCycle))
                return null;

            var normalized = billCycle.Trim();

            if ((normalized.StartsWith("{") && normalized.EndsWith("}")) ||
                normalized.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                return null;

            return normalized;
        }

        private static string NormalizeRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return null;

            var normalized = region.Trim().ToUpperInvariant();
            return normalized == "ALL" ? null : normalized;
        }

        private static string NormalizeProvince(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return null;

            var normalized = province.Trim().ToUpperInvariant();
            return normalized == "ALL" ? null : normalized;
        }

        private static string NormalizeArea(string area)
        {
            if (string.IsNullOrWhiteSpace(area))
                return null;

            var normalized = area.Trim().ToUpperInvariant();
            return normalized == "ALL" ? null : normalized;
        }
    }


    // =========================================================================
    // 3. SOLAR BULK CUSTOMERS CONTROLLER
    //    Routes: api/dashboard/solar-bulk-customers/...
    // =========================================================================
    [RoutePrefix("api/dashboard/solar-bulk-customers")]
    public class SolarBulkCustomersController : ApiController
    {
        private readonly SolarBulkCustomersDao _solarBulkCustomersDao = new SolarBulkCustomersDao();

        /// <summary>GET api/dashboard/solar-bulk-customers/summary</summary>
        [HttpGet]
        [Route("summary")]
        public IHttpActionResult GetSummary([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            try
            {
                if (!_solarBulkCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = _solarBulkCustomersDao.GetSummary(NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving solar bulk customers summary.", errorDetails = ex.Message });
            }
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count</summary>
        [HttpGet]
        [Route("count")]
        public IHttpActionResult GetTotalCustomersCount([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCountResponse(_solarBulkCustomersDao.GetTotalCustomersCount, "Error retrieving total solar bulk customers count.", region, province, area);
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count/net-type-1</summary>
        [HttpGet]
        [Route("count/net-type-1")]
        public IHttpActionResult GetNetType1CustomersCount([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCountResponse(_solarBulkCustomersDao.GetNetType1CustomersCount, "Error retrieving net type 1 solar bulk customers count.", region, province, area);
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count/net-type-2</summary>
        [HttpGet]
        [Route("count/net-type-2")]
        public IHttpActionResult GetNetType2CustomersCount([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCountResponse(_solarBulkCustomersDao.GetNetType2CustomersCount, "Error retrieving net type 2 solar bulk customers count.", region, province, area);
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count/net-type-3</summary>
        [HttpGet]
        [Route("count/net-type-3")]
        public IHttpActionResult GetNetType3CustomersCount([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCountResponse(_solarBulkCustomersDao.GetNetType3CustomersCount, "Error retrieving net type 3 solar bulk customers count.", region, province, area);
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/count/net-type-4</summary>
        [HttpGet]
        [Route("count/net-type-4")]
        public IHttpActionResult GetNetType4CustomersCount([FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            return GetCountResponse(_solarBulkCustomersDao.GetNetType4CustomersCount, "Error retrieving net type 4 solar bulk customers count.", region, province, area);
        }

        /// <summary>GET api/dashboard/solar-bulk-customers/generation-capacity?billCycle=401&cycles=12</summary>
        [HttpGet]
        [Route("generation-capacity")]
        public IHttpActionResult GetGenerationCapacityGraph([FromUri] string billCycle = null, [FromUri] int cycles = 12, [FromUri] string region = null, [FromUri] string province = null, [FromUri] string area = null)
        {
            try
            {
                if (!_solarBulkCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = _solarBulkCustomersDao.GetGenerationCapacityGraph(NormalizeBillCycle(billCycle), cycles, NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = "Error retrieving solar bulk generation capacity graph.", errorDetails = ex.Message });
            }
        }

        // ── Shared helper ─────────────────────────────────────────────────────
        private IHttpActionResult GetCountResponse(
            Func<string, string, string, SolarBulkCustomersCount> countGetter,
            string fallbackErrorMessage,
            string region,
            string province,
            string area)
        {
            try
            {
                if (!_solarBulkCustomersDao.TestConnection(out string connError))
                    return Ok(new { data = (object)null, errorMessage = "Database connection failed.", errorDetails = connError });

                var data = countGetter(NormalizeRegion(region), NormalizeProvince(province), NormalizeArea(area));

                return Ok(new
                {
                    data,
                    errorMessage = string.IsNullOrWhiteSpace(data.ErrorMessage) ? (string)null : data.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return Ok(new { data = (object)null, errorMessage = fallbackErrorMessage, errorDetails = ex.Message });
            }
        }

        private static string NormalizeBillCycle(string billCycle)
        {
            if (string.IsNullOrWhiteSpace(billCycle))
                return null;

            var normalized = billCycle.Trim();

            if ((normalized.StartsWith("{") && normalized.EndsWith("}")) ||
                normalized.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                return null;

            return normalized;
        }

        private static string NormalizeRegion(string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return null;

            var normalized = region.Trim().ToUpperInvariant();
            return normalized == "ALL" ? null : normalized;
        }

        private static string NormalizeProvince(string province)
        {
            if (string.IsNullOrWhiteSpace(province))
                return null;

            var normalized = province.Trim().ToUpperInvariant();
            return normalized == "ALL" ? null : normalized;
        }

        private static string NormalizeArea(string area)
        {
            if (string.IsNullOrWhiteSpace(area))
                return null;

            var normalized = area.Trim().ToUpperInvariant();
            return normalized == "ALL" ? null : normalized;
        }
    }
}