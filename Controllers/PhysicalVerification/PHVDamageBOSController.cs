using System.Threading.Tasks;
using System.Web.Http;
using MISReports_Api.DAL.PhysicalVerification;

namespace MISReports_Api.Controllers.PhysicalVerification
{
    [RoutePrefix("api/phv-damage-obs")]
    public class PHVDamageOBSController : ApiController
    {
        private readonly PHVDamageOBSRepository _repository =
            new PHVDamageOBSRepository();

        [HttpGet]
        [Route("list")]
        public async Task<IHttpActionResult> GetDamageOBS(
            string deptId,
            string warehouseCode,
            int repYear,
            int repMonth)
        {
            var data = await _repository.GetDamageOBSAsync(
                deptId, warehouseCode, repYear, repMonth);

            if (data.Count == 0)
                return Ok(new { message = "No data found." });

            return Ok(data);
        }
    }
}