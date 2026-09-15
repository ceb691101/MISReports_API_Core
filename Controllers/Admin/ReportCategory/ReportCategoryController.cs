using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/reportcategory")]
    public class ReportCategoryController : ApiController
    {
        private readonly ReportCategoryRepository _repository = new ReportCategoryRepository();

        private static string NormalizeCategoryCode(string catCode)
        {
            return string.IsNullOrWhiteSpace(catCode)
                ? null
                : catCode.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Get all report categories
        /// </summary>
        /// <returns>List of all report categories</returns>
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllCategories()
        {
            try
            {
                var result = _repository.GetAllCategories();

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = result,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "CANNOT GET REPORT CATEGORIES.",
                    errorDetails = ex.Message
                })));
            }
        }

        /// <summary>
        /// Get a specific report category by code
        /// </summary>
        /// <param name="catCode">The category code</param>
        /// <returns>Report category details</returns>
        [HttpGet]
        [Route("{catCode}")]
        public IHttpActionResult GetCategoryByCode(string catCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(catCode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "CATEGORY CODE IS REQUIRED."
                    }));
                }

                var result = _repository.GetCategoryByCode(NormalizeCategoryCode(catCode));

                if (result == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "CATEGORY NOT FOUND."
                    }));
                }

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "CANNOT GET REPORT CATEGORY.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Create or update a report category
        /// </summary>
        /// <param name="request">CreateReportCategoryRequest object</param>
        /// <returns>Success/failure message</returns>
        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateCategory([FromBody] CreateReportCategoryRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "REQUEST BODY IS REQUIRED."
                    }));
                }

                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.CatCode))
                    validationErrors.Add("CATEGORY CODE IS REQUIRED.");

                if (string.IsNullOrWhiteSpace(request.CatName))
                    validationErrors.Add("CATEGORY NAME IS REQUIRED.");

                if (validationErrors.Count > 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = string.Join(" ", validationErrors)
                    }));
                }

                var created = _repository.AddOrUpdateCategory(request);
                var normalizedCatCode = NormalizeCategoryCode(request.CatCode);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = created,
                        catCode = normalizedCatCode,
                        message = "CATEGORY CREATED/UPDATED SUCCESSFULLY."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "CANNOT CREATE/UPDATE CATEGORY.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Update a report category
        /// </summary>
        /// <param name="catCode">The category code to update</param>
        /// <param name="request">CreateReportCategoryRequest object</param>
        /// <returns>Success/failure message</returns>
        [HttpPut]
        [Route("{catCode}")]
        public IHttpActionResult UpdateCategory(string catCode, [FromBody] CreateReportCategoryRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "REQUEST BODY IS REQUIRED."
                    }));
                }

                if (string.IsNullOrWhiteSpace(catCode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "CATEGORY CODE IS REQUIRED."
                    }));
                }

                // Set the category code from the URL if not provided in the request
                if (string.IsNullOrWhiteSpace(request.CatCode))
                {
                    request.CatCode = NormalizeCategoryCode(catCode);
                }

                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.CatCode))
                    validationErrors.Add("CATEGORY CODE IS REQUIRED.");

                if (string.IsNullOrWhiteSpace(request.CatName))
                    validationErrors.Add("CATEGORY NAME IS REQUIRED.");

                if (validationErrors.Count > 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = string.Join(" ", validationErrors)
                    }));
                }

                var updated = _repository.UpdateCategory(request);
                var normalizedCatCode = NormalizeCategoryCode(request.CatCode);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = updated,
                        catCode = normalizedCatCode,
                        message = updated ? "CATEGORY UPDATED SUCCESSFULLY." : "CATEGORY NOT FOUND."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "CANNOT UPDATE CATEGORY.",
                    errorDetails = ex.Message
                }));
            }
        }

        /// <summary>
        /// Delete a report category
        /// </summary>
        /// <param name="catCode">The category code to delete</param>
        /// <returns>Success/failure message</returns>
        [HttpDelete]
        [Route("{catCode}")]
        public IHttpActionResult DeleteCategory(string catCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(catCode))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "CATEGORY CODE IS REQUIRED."
                    }));
                }

                var normalizedCatCode = NormalizeCategoryCode(catCode);
                var deleted = _repository.DeleteCategory(normalizedCatCode);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = deleted,
                        catCode = normalizedCatCode,
                        message = deleted ? "CATEGORY DELETED SUCCESSFULLY." : "CATEGORY NOT FOUND."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "CANNOT DELETE CATEGORY.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}








