using MISReports_Api.DAL.SolarInformation.SolarProgressClarification;
using MISReports_Api.DAL.SolarInformation.SolarPVConnections;
using MISReports_Api.DAL.SolarInformation.SolarPaymentRetail;
using MISReports_Api.DAL.SolarInformation.SolarPVCapacity;
using MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff;
using MISReports_Api.DAL.General.ArrearsPosition; // ← ADDED
using MISReports_Api.DAL.Shared;
using MISReports_Api.DAL;
using Newtonsoft.Json.Linq;
using System;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api")]
    public class SharedController : ApiController
    {
        private readonly AreasDao _areasDao = new AreasDao();
        private readonly ProvinceDao _provinceDao = new ProvinceDao();
        private readonly RegionDao _regionDao = new RegionDao();
        private readonly BillCycleDao _billCycleDao = new BillCycleDao();
        private readonly PVBillCycleDao _pvBillCycleDao = new PVBillCycleDao();
        private readonly ProvinceOrdinaryDao _provinceOrdinaryDao = new ProvinceOrdinaryDao();
        private readonly RegionOrdinaryDao _regionOrdinaryDao = new RegionOrdinaryDao();
        private readonly BillCycleOrdinaryDao _billCycleOrdinaryDao = new BillCycleOrdinaryDao();
        private readonly BillCycleRetailDao _billCycleRetailDao = new BillCycleRetailDao();
        private readonly AreasRepository _areasRepository = new AreasRepository();
        private readonly PVCapacityBillCycleDao _pVCapacityBillCycleDao = new PVCapacityBillCycleDao();
        private readonly BillCycleFromAreaDao _billCycleFromAreaDao = new BillCycleFromAreaDao();
        private readonly ContractDemandBillCycleDao _contractDemandBillCycle = new ContractDemandBillCycleDao();
        private readonly ActiveCustSalesOrdBillCycleDao _activeCustSalesOrdBillCycle = new ActiveCustSalesOrdBillCycleDao();
        private readonly ActiveCustSalesBulkBillCycleDao _activeCustSalesBulkBillCycle = new ActiveCustSalesBulkBillCycleDao();
        private readonly CalcCycleFromAreaDao _calcCycleFromAreaDao = new CalcCycleFromAreaDao();
        private readonly GovernmentAccountsBillCycleDao _govAccountsBillCycleDao = new GovernmentAccountsBillCycleDao();
        private readonly ArrearsPositionBillCycleDao _arrearsPositionBillCycleDao = new ArrearsPositionBillCycleDao(); // ← ADDED
        private readonly ReceivablePositionBillCycleDao _receivablePositionBillCycleDao = new ReceivablePositionBillCycleDao();

        [HttpGet]
        [Route("ordinary/areas")]
        public IHttpActionResult GetAreas([FromUri] string regionCode = null, [FromUri] string provCode = null)
        {
            try
            {
                if (!_areasDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var areas = _areasRepository.GetAreas(regionCode, provCode);

                return Ok(JObject.FromObject(new
                {
                    data = areas,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/areas")]
        public IHttpActionResult GetBulkAreas([FromUri] string regionCode = null, [FromUri] string provCode = null)
        {
            try
            {
                if (!_areasDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var areas = _areasDao.GetAreas(regionCode, provCode);

                return Ok(JObject.FromObject(new
                {
                    data = areas,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get areas data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/province")]
        public IHttpActionResult GetProvince([FromUri] string regionCode = null)
        {
            try
            {
                if (!_provinceDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var province = _provinceDao.GetProvince(regionCode);

                return Ok(JObject.FromObject(new
                {
                    data = province,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get province data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/province")]
        public IHttpActionResult GetOrdinaryProvince([FromUri] string regionCode = null)
        {
            try
            {
                if (!_provinceOrdinaryDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var province = _provinceOrdinaryDao.GetProvince(regionCode);

                return Ok(JObject.FromObject(new
                {
                    data = province,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get province data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/region")]
        public IHttpActionResult GetRegion()
        {
            try
            {
                if (!_regionDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var region = _regionDao.GetRegion();

                return Ok(JObject.FromObject(new
                {
                    data = region,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get region data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/region")]
        public IHttpActionResult GetOrdinaryRegion()
        {
            try
            {
                if (!_regionOrdinaryDao.TestConnection(out string connError))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Database connection failed.",
                        errorDetails = connError
                    }));
                }

                var region = _regionOrdinaryDao.GetRegion();

                return Ok(JObject.FromObject(new
                {
                    data = region,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get region data.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/netmtchg/billcycle/max")]
        public IHttpActionResult GetMaxBillCycle()
        {
            try
            {
                var result = _billCycleDao.GetLast24BillCycles(); // From netmtchg table in InformixBulkConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/netmtcons/billcycle/max")]
        public IHttpActionResult GetPVBillCycle()
        {
            try
            {
                var result = _pvBillCycleDao.GetLast24BillCycles(); // From netmtcons table in InformixBulkConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/netmtchg/billcycle/max")]
        public IHttpActionResult GetOrdinaryBillCycle()
        {
            try
            {
                var result = _billCycleOrdinaryDao.GetLast24BillCycles(); // From netmtchg table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/netmtcons/billcycle/max")]
        public IHttpActionResult GetRetailBillCycle()
        {
            try
            {
                var result = _billCycleRetailDao.GetLast24BillCycles(); // From netmtcons table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/netprogrs/billcycle/max")]
        public IHttpActionResult GetPVCapacityMaxBillCycle()
        {
            try
            {
                var result = _pVCapacityBillCycleDao.GetLast24BillCycles(); // From netprogrs table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/areas/billcycle/min")]
        public IHttpActionResult GetBillCycleFromArea()
        {
            try
            {
                var result = _billCycleFromAreaDao.GetLast24BillCycles(); // From areas table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle from areas",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/mon_tot/billcycle/max")]
        public IHttpActionResult GetContractDemandBillCycle()
        {
            try
            {
                var result = _contractDemandBillCycle.GetLast24BillCycles(); // From mon_tot table in InformixBulkConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/consmry/billcycle/max")]
        public IHttpActionResult GetActiveCustomersSalesOrdBillCycle()
        {
            try
            {
                var result = _activeCustSalesOrdBillCycle.GetLast36BillCycles(); // From consmry table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("bulk/account_info/billcycle/max")]
        public IHttpActionResult GetActiveCustomersSalesBulkBillCycle()
        {
            try
            {
                var result = _activeCustSalesBulkBillCycle.GetLast36BillCycles(); // From account_info table in InformixBulkConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("ordinary/areas/calccycle/max")]
        public IHttpActionResult GetCalcCycleFromArea()
        {
            try
            {
                var result = _calcCycleFromAreaDao.GetMaxCalcCycle(); // From areas table in InformixConnection database

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max calc cycle from areas",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Returns the maximum bill cycle from prn_dat_1 for a given area.
        /// Used by the Government Accounts report to seed the bill-cycle dropdown.
        /// DB: billsmry (bulk connection).
        /// </summary>
        // GET api/billsmry/prn_dat_1/billcycle/max?areaCode=43
        // Response: { data: { maxBillCycle: "438" }, errorMessage: null }
        [HttpGet]
        [Route("billsmry/prn_dat_1/billcycle/max")]
        public IHttpActionResult GetGovernmentAccountsBillCycle([FromUri] string areaCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Area code is required."
                    }));

                var result = _govAccountsBillCycleDao.GetMaxBillCycle(areaCode);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = result.ErrorMessage
                    }));

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle for Government Accounts.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Returns the last 24 bill cycles from the receivable_position table.
        /// Used by the Receivable Position report to seed the bill-cycle dropdown.
        /// DB: receivable_position (bulk connection).
        /// </summary>
        // GET api/receivable-position/billcycle/max
        // Response: { data: { maxBillCycle: "454", billCycles: [...] }, errorMessage: null }
        [HttpGet]
        [Route("receivable-position/billcycle/max")]
        public IHttpActionResult GetReceivablePositionBillCycle([FromUri] string billType = null)
        {
            try
            {
                var result = _receivablePositionBillCycleDao.GetLast24BillCycles(billType);

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = result.BillCycles != null && result.BillCycles.Count > 0
                        ? (string)null
                        : result.ErrorMessage
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle for Receivable Position.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Returns the maximum bill cycle from the <c>areas</c> table for a given area code.
        /// Used by the Arrears Position (meter-reader wise) report to seed the bill-cycle dropdown.
        /// DB: billsmry (bulk connection).
        /// </summary>
        // GET api/billsmry/areas/arrears-position/billcycle/max?areaCode=43
        // Response: { data: { maxBillCycle: "438", billCycles: [...] }, errorMessage: null }
        [HttpGet]
        [Route("billsmry/areas/arrears-position/billcycle/max")]
        public IHttpActionResult GetArrearsPositionBillCycle([FromUri] string areaCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(areaCode))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Area code is required."
                    }));

                var result = _arrearsPositionBillCycleDao.GetMaxBillCycle(areaCode);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = result.ErrorMessage
                    }));

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get max bill cycle for Arrears Position.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}