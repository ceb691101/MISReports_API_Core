//12.PIV Search
// File: PivSearchController.cs
using MISReports_Api.Compat;
using MISReports_Api.DAL.PIV;
using System;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/pivsearch")]
    public class PivSearchController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly PivSearchRepository _repo = new PivSearchRepository();

        // GET: api/pivsearch/get?piv=ABC123&project=PROJ001
        // Either piv or project or both can be provided
        [HttpGet]
        [Route("get")]
        public IActionResult Get(string piv = null, string project = null)
        {
            if (string.IsNullOrWhiteSpace(piv) && string.IsNullOrWhiteSpace(project))
            {
                return BadRequest("At least one of 'piv' or 'project' parameter is required.");
            }

            try
            {
                var data = _repo.GetPivDetails(piv, project);
                return Ok(data);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Get failed. piv={piv}, project={project}", piv, project);
                return InternalServerError(new Exception("Error while fetching PIV details: " + ex.Message));
            }
        }
    }
}