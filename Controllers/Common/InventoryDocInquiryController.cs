using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using MISReports_Api.DAL;
using MISReports_Api.Models.Accounts;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/inventorydocinquiry")]
    public class InventoryDocInquiryController : ApiController
    {
        private readonly InventoryDocInquiryDAL _dal = new InventoryDocInquiryDAL();

        // PATH: /api/inventorydocinquiry/report/510.11%2FISS%2F26%2F0001
        [HttpGet]
        [Route("report/{docNo}")]
        public IHttpActionResult GetReport(string docNo)
        {
            return ExecuteQuery(docNo);
        }

        // QUERY: /api/inventorydocinquiry/report?docNo=510.11/ISS/26/0001
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string docNo)
        {
            return ExecuteQuery(docNo);
        }

        private IHttpActionResult ExecuteQuery(string docNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docNo))
                    return BadRequest("docNo is required.");

                var data = _dal.GetInventoryDocInquiry(docNo.Trim());
                var totalTrxVal = data.Sum(x => x.TrxVal ?? 0m);

                var summary = new
                {
                    docNo = docNo.Trim(),
                    totalRecords = data.Count,
                    totalTrxVal
                };

                return Ok(new
                {
                    success = true,
                    message = data.Any() ? "Data retrieved successfully" : "No records found",
                    data,
                    summary
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
                return InternalServerError(new Exception($"Database error: {ex.Message}", ex));
            }
        }
    }
}