using MISReports_Api.DAL;
using MISReports_Api.Models;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/jobsearchmaintable")]
    public class JobSearchMainTableController : ApiController
    {
        private readonly JobSearchMainTableRepository _repository = new JobSearchMainTableRepository();

        // GET: api/jobsearchmaintable
        // Example calls:
        // ?applicationNo=APP20230045
        // ?accountNo=900123456
        // ?tele=0777123456
        // ?projectNo=PJ-45678
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Search(
            [FromUri] string applicationId = null,
            [FromUri] string applicationNo = null,
            [FromUri] string projectNo = null,
            [FromUri] string idNo = null,
            [FromUri] string accountNo = null,
            [FromUri] string tele = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(applicationId) &&
                    string.IsNullOrWhiteSpace(applicationNo) &&
                    string.IsNullOrWhiteSpace(projectNo) &&
                    string.IsNullOrWhiteSpace(idNo) &&
                    string.IsNullOrWhiteSpace(accountNo) &&
                    string.IsNullOrWhiteSpace(tele))
                {
                    return BadRequest("Please provide at least one search parameter.");
                }

                var results = await _repository.SearchAsync(applicationId,
                                                            applicationNo,
                                                            projectNo,
                                                            idNo,
                                                            accountNo,
                                                            tele);

                return Ok(new
                {
                    success = true,
                    count = results.Count,
                    data = results
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"JobSearchMainTable error: {ex}");
                return Ok(new
                {
                    success = false,
                    message = "Error during job search",
                    detail = ex.Message
                });
            }
        }
    }
}