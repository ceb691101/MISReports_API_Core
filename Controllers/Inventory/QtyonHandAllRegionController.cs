using MISReports_Api.DAL.Inventory;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MISReports_Api.Controllers.Inventory
{
    [RoutePrefix("api/qty-on-hand-all-region")]
    public class QtyonHandAllRegionController : ApiController
    {
        private readonly QtyonHandAllRegionRepository _repository = new QtyonHandAllRegionRepository();

        // GET api/qty-on-all-region?compId=XXX&matcode=YYY
        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> GetQtyonHandAllRegion(
            [FromUri] string compId,
            [FromUri] string matcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(compId))
                    return BadRequest("Company Id is required");

                if (string.IsNullOrWhiteSpace(matcode))
                    matcode = "";

                System.Diagnostics.Trace.WriteLine($"Request received: compId={compId}, matcode={matcode}");

                var data = await _repository.GetQtyonHandAllRegionAsync(compId, matcode);

                return Ok(new
                {
                    success = true,
                    count = data.Count,
                    data = data
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR: {ex}");

                return Ok(new
                {
                    success = false,
                    message = "Error retrieving Qty On Hand data",
                    detailedError = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}