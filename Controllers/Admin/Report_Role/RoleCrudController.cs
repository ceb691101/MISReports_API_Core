using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
	[RoutePrefix("api/role-report")]
	public class RoleCrudController : ApiController
	{
		private readonly RoleCrudRepository _repository = new RoleCrudRepository();

		[HttpPost]
		[Route("reports/by-category")]
		public IHttpActionResult GetReportsByCategory([FromBody] ReportsByCategoryRequest request)
		{
			try
			{
				if (request == null)
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "Request body is required."
					}));
				}

				var mode = request.AddReports?.Trim();
				if (!string.IsNullOrWhiteSpace(mode) && !string.Equals(mode, "byRepCat", StringComparison.OrdinalIgnoreCase))
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "Unsupported addReports mode. Use byRepCat."
					}));
				}

				var catCodes = (request.CatCodes ?? new List<string>())
					.Where(code => !string.IsNullOrWhiteSpace(code))
					.Select(code => code.Trim())
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();

				if (catCodes.Count == 0)
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "At least one CatCode is required."
					}));
				}

				var result = _repository.GetReportsByCategory(catCodes);

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
					errorMessage = "Cannot get reports by category.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpPost]
		[Route("save-userrolereps")]
		public IHttpActionResult SaveUserRoleReports([FromBody] SaveUserRoleReportsRequest request)
		{
			try
			{
				if (request == null)
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "Request body is required."
					}));
				}

				if (string.IsNullOrWhiteSpace(request.RoleId))
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "RoleId is required."
					}));
				}

				var reports = request.Reports ?? new List<RoleReportItemRequest>();
				if (reports.Count == 0 && (request.CatCodes?.Count ?? 0) > 0)
				{
					var byCategory = _repository.GetReportsByCategory(request.CatCodes);
					reports = byCategory.Select(item => new RoleReportItemRequest
					{
						CatCode = item.CatCode,
						RepId = item.RepId,
						Favorite = "1"
					}).ToList();
				}

				if (reports.Count == 0)
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "At least one report is required."
					}));
				}

				var saveResult = _repository.SaveUserRoleReports(request.RoleId.Trim(), reports);

				return Ok(JObject.FromObject(new
				{
					data = new
					{
						roleId = request.RoleId.Trim(),
						inserted = saveResult.Inserted,
						updated = saveResult.Updated,
						ignored = saveResult.Ignored,
						message = "Role reports saved successfully."
					},
					errorMessage = (string)null
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot save role reports.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpDelete]
		[Route("user/{roleId}/reports")]
		public IHttpActionResult DeleteAllReports(string roleId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(roleId))
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "RoleId is required."
					}));
				}

				var deleted = _repository.DeleteAllReports(roleId.Trim());
				return Ok(JObject.FromObject(new
				{
					data = new { roleId = roleId.Trim(), deletedRows = deleted },
					errorMessage = (string)null
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot delete reports.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpDelete]
		[Route("user/{roleId}/reports/category/{catCode}")]
		public IHttpActionResult DeleteReportsByCategory(string roleId, string catCode)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(catCode))
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "RoleId and CatCode are required."
					}));
				}

				var deleted = _repository.DeleteReportsByCategory(roleId.Trim(), catCode.Trim());
				return Ok(JObject.FromObject(new
				{
					data = new { roleId = roleId.Trim(), catCode = catCode.Trim(), deletedRows = deleted },
					errorMessage = (string)null
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot delete reports by category.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpDelete]
		[Route("user/{roleId}/reports/{repId}")]
		public IHttpActionResult DeleteReportByName(string roleId, string repId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(roleId) || string.IsNullOrWhiteSpace(repId))
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "RoleId and RepId are required."
					}));
				}

				var deleted = _repository.DeleteReportByName(roleId.Trim(), repId.Trim());
				return Ok(JObject.FromObject(new
				{
					data = new { roleId = roleId.Trim(), repId = repId.Trim(), deletedRows = deleted },
					errorMessage = (string)null
				}));
			}
			catch (Exception ex)
			{
				return Ok(JObject.FromObject(new
				{
					data = (object)null,
					errorMessage = "Cannot delete report by name.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpGet]
		[Route("reports/{repId}")]
		public IHttpActionResult GetReportsByName(string repId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(repId))
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "RepId is required."
					}));
				}

				var result = _repository.GetReportsByName(repId.Trim());
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
					errorMessage = "Cannot get report by name.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpGet]
		[Route("categories")]
		public IHttpActionResult GetAllCategories()
		{
			try
			{
				var result = _repository.GetAllCategories();
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
					errorMessage = "Cannot get categories.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpGet]
		[Route("reports")]
		public IHttpActionResult GetAllActiveReports()
		{
			try
			{
				var result = _repository.GetAllActiveReports();
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
					errorMessage = "Cannot get active reports.",
					errorDetails = ex.Message
				}));
			}
		}

		[HttpGet]
		[Route("user/{roleId}/reports")]
		public IHttpActionResult GetUserAssignedReports(string roleId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(roleId))
				{
					return Ok(JObject.FromObject(new
					{
						data = (object)null,
						errorMessage = "RoleId is required."
					}));
				}

				var result = _repository.GetUserAssignedReports(roleId.Trim());
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
					errorMessage = "Cannot get user assigned reports.",
					errorDetails = ex.Message
				}));
			}
		}
	}
}
