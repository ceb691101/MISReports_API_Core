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
    [RoutePrefix("api/quantitymatfifo")]
    public class QuantityMatFIFOController : ApiController
    {
        private readonly QuantityMatFIFODAL _dal = new QuantityMatFIFODAL();

        // PATH: /api/quantitymatfifo/report/980.90/WRH-TCP-MAIN/RM
        // matCode is optional
        [HttpGet]
        [Route("report/{costCtr}/{whCode}")]
        public IHttpActionResult GetReport(string costCtr, string whCode)
        {
            return ExecuteQuery(costCtr, whCode, null);
        }

        // PATH: /api/quantitymatfifo/report/980.90/WRH-TCP-MAIN/
        [HttpGet]
        [Route("report/{costCtr}/{whCode}/{matCode}")]
        public IHttpActionResult GetReport(string costCtr, string whCode, string matCode)
        {
            return ExecuteQuery(costCtr, whCode, matCode);
        }

        // QUERY: /api/quantitymatfifo/report?costCtr=980.90&whCode=WRH_TCP_MAIN&matCode=
        // matCode is optional
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string costCtr, [FromUri] string whCode, [FromUri] string matCode = null)
        {
            return ExecuteQuery(costCtr, whCode, matCode);
        }

        private IHttpActionResult ExecuteQuery(string costCtr, string whCode, string matCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");
                if (string.IsNullOrWhiteSpace(whCode))
                    return BadRequest("whCode is required.");

                string costCtrTrimmed = costCtr.Trim();
                string whCodeTrimmed = whCode.Trim();

                // matCode is a LIKE prefix filter
                string matCodeTrimmed = (matCode ?? string.Empty).Trim();

                var data = _dal.GetQuantityMatFIFO(costCtrTrimmed, whCodeTrimmed, matCodeTrimmed);
                const int MAX_RECORDS = 5000;

                var summary = new
                {
                    costCtr = costCtrTrimmed,
                    whCode = whCodeTrimmed,
                    matCode = matCodeTrimmed,
                    totalRecords = data.Count,
                    totalQtyOnHand = data.Sum(x => x.QtyOnHand ?? 0m),
                    totalValue = data.Sum(x => x.Value ?? 0m)
                };

                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records ({data.Count}). Result capped at {MAX_RECORDS}. Use narrower filters.",
                        data = new object[0],
                        summary
                    });
                }

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