using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProvinceMaterialReqDetailController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }
    }
}