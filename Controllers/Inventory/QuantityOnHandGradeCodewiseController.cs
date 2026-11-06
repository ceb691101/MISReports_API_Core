using MISReports_Api.Compat;
using MISReports_Api.DAL.Inventory;
using MISReports_Api.Models.Inventory;
using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers.Inventory
{
    [RoutePrefix("api/quantity-on-hand-grade-codewise")]
    public class QuantityOnHandGradeCodewiseController : ApiController
    {
        private readonly QuantityOnHandGradeCodewiseDAL _repository =
            new QuantityOnHandGradeCodewiseDAL();

        // ---------------------- REPORT ENDPOINT ----------------------

        [HttpGet]
        [Route("report/{costCenter}/{gradeCode}/{matCode?}")]
        public IActionResult GetQuantityOnHandGradeCodewise(
            string costCenter,
            string gradeCode,
            string matCode = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(costCenter))
                {
                    return BadRequest("Cost center cannot be empty.");
                }

                if (string.IsNullOrWhiteSpace(gradeCode))
                {
                    return BadRequest("Grade code cannot be empty.");
                }

                var result = _repository.GetQuantityOnHandGradeCodewise(
                    costCenter.Trim(),
                    string.IsNullOrWhiteSpace(matCode) ? string.Empty : matCode.Trim(),
                    gradeCode.Trim()
                );

                return Ok(new QuantityOnHandGradeCodewiseResponse
                {
                    Data = result,
                    ErrorMessage = null,
                    ErrorDetails = null
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"Error in GetQuantityOnHandGradeCodewise: {ex}"
                );

                return InternalServerError(ex);
            }
        }
    }
}