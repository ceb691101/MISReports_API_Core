using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/constructioncompleted/lookups")]
    public class ConstructionCompletedLookupController : ApiController
    {
        private readonly ConstructionCompletedLookupDAL _dal = new ConstructionCompletedLookupDAL();

        // GET: /api/constructioncompleted/lookups/fundids
        [HttpGet]
        [Route("fundids")]
        public IHttpActionResult GetFundIds()
        {
            try
            {
                var data = _dal.GetFundIds();
                return Ok(new { success = true, message = data.Any() ? "Data retrieved successfully" : "No records found", data });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }

        // GET: /api/constructioncompleted/lookups/districts/{roleId}
        [HttpGet]
        [Route("districts/{roleId}")]
        public IHttpActionResult GetDistricts(string roleId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleId))
                    return BadRequest("roleId is required.");

                var data = _dal.GetDistricts(roleId.Trim());
                return Ok(new { success = true, message = data.Any() ? "Data retrieved successfully" : "No records found", data });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}