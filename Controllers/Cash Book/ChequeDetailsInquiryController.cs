using System;
using System.Linq;
using System.Web.Http;
using MISReports_Api.DAL;
namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/chequedetailsinquiry")]
    public class ChequeDetailsInquiryController : ApiController
    {
        private readonly ChequeDetailsInquiryDAL _dal = new ChequeDetailsInquiryDAL();

        // PATH: /api/chequedetailsinquiry/report/100/1/20
        [HttpGet]
        [Route("report/{costCtr}/{fromNo}/{toNo}")]
        public IHttpActionResult GetReport(string costCtr, string fromNo, string toNo)
        {
            return ExecuteQuery(costCtr, fromNo, toNo);
        }

        // QUERY: /api/chequedetailsinquiry/report?costCtr=100&fromNo=1&toNo=20
        [HttpGet]
        [Route("report")]
        public IHttpActionResult GetQuery([FromUri] string costCtr, [FromUri] string fromNo, [FromUri] string toNo)
        {
            return ExecuteQuery(costCtr, fromNo, toNo);
        }

        private IHttpActionResult ExecuteQuery(string costCtr, string fromNo, string toNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCtr))
                    return BadRequest("costCtr is required.");
                if (string.IsNullOrWhiteSpace(fromNo) || !int.TryParse(fromNo, out int fromNum))
                    return BadRequest("fromNo must be a valid integer.");
                if (string.IsNullOrWhiteSpace(toNo) || !int.TryParse(toNo, out int toNum))
                    return BadRequest("toNo must be a valid integer.");
                if (toNum < fromNum)
                    return BadRequest("toNo cannot be less than fromNo.");

                var data = _dal.GetChequeDetailsInquiry(costCtr.Trim(), fromNum.ToString(), toNum.ToString());

                const int MAX_RECORDS = 5000;
                var summary = new
                {
                    costCtr = costCtr.Trim(),
                    fromNo = fromNum,
                    toNo = toNum,
                    totalRecords = data.Count
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