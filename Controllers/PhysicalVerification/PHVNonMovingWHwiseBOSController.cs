using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MISReports_Api.DAL.PhysicalVerification;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/phv-nonmovingwhwisebos-report")]
    public class PHVNonMovingWHwiseBOSController : ApiController
    {
        private readonly PHVNonMovingWHwiseBOSRepository _repository;

        public PHVNonMovingWHwiseBOSController()
        {
            _repository = new PHVNonMovingWHwiseBOSRepository();
        }

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetNonMovingReport(
            string deptId,
            int repYear,
            int repMonth,
            string warehouseCode)
        {
            if (string.IsNullOrWhiteSpace(deptId) || string.IsNullOrWhiteSpace(warehouseCode))
                return BadRequest("Department Id and Warehouse Code are required.");

            try
            {
                var data = await _repository.GetNonMovingWHwiseBOSAsync(
                    deptId.Trim(),
                    warehouseCode.Trim(),
                    repYear,
                    repMonth
                );

                if (data.Count == 0)
                    return Ok(new { message = "No data found." });

                return Ok(data);
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException ex)
            {
                if (ex.Number == 1652)
                    return InternalServerError(new Exception("Database TEMP tablespace full."));

                return InternalServerError(ex);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}