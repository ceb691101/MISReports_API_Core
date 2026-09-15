using System;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/glinquirybydoc")]
    public class GlInquiryByDocController : ApiController
    {
        private readonly GlInquiryByDocDAL _dal = new GlInquiryByDocDAL();

        // QUERY ONLY: /api/glinquirybydoc/report?docNo=914.00%2FPSA%2F26%2F0100
        // (docNo can contain '/', which is unsafe as a route segment, so path-style routing is not offered here)
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string docNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(docNo))
                    return BadRequest("docNo is required.");

                var data = _dal.GetGlInquiryByDoc(docNo.Trim());
                var totalTrxVal = data.Sum(x => x.TrxVal ?? 0m);
                var totalDrAmt = data.Sum(x => x.DrAmt ?? 0m);
                var totalCrAmt = data.Sum(x => x.CrAmt ?? 0m);

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    docNo = docNo.Trim(),
                    totalRecords = data.Count,
                    totalTrxVal,
                    totalDrAmt,
                    totalCrAmt
                };

                if (data.Count >= MAX_RECORDS)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Too many records ({data.Count}). Result capped at {MAX_RECORDS}. Use a narrower search.",
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