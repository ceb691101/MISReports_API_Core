using MISReports_Api.Compat;
using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/roleinfo")]
    public class RoleInfoController : ApiController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly RoleInfoRepository _repository = new RoleInfoRepository();

        private static string NormalizeUpper(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private static bool HasCostCentres(CreateRoleRequest request)
        {
            return (request?.CostCentres != null && request.CostCentres.Exists(value => !string.IsNullOrWhiteSpace(value)))
                || !string.IsNullOrWhiteSpace(request?.CostCentre);
        }

        [HttpGet]
        [Route("admin")]
        public IActionResult GetAdminRoles()
        {
            try
            {
                var result = _repository.GetAdminRoles();

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = result,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAdminRoles failed");
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get Admin Roles.",
                    errorDetails = ex.Message
                })));
            }
        }

        [HttpGet]
        [Route("user")]
        public IActionResult GetUserRoles()
        {
            try
            {
                var result = _repository.GetUserRoles();

                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = result,
                    errorMessage = (string)null
                })));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetUserRoles failed");
                return Ok(JObject.Parse(JsonConvert.SerializeObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get User Roles.",
                    errorDetails = ex.Message
                })));
            }
        }

        [HttpPost]
        [Route("")]
        public IActionResult CreateRole([FromBody] CreateRoleRequest request)
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

                request.RoleId = NormalizeUpper(request.RoleId);
                request.UserType = NormalizeUpper(request.UserType);
                request.OriginalUserType = NormalizeUpper(request.OriginalUserType);

                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.EpfNo))
                    validationErrors.Add("EpfNo is required.");

                if (string.IsNullOrWhiteSpace(request.RoleId))
                    validationErrors.Add("RoleId is required.");

                if (string.IsNullOrWhiteSpace(request.UserType))
                    validationErrors.Add("UserType is required.");

                if (string.IsNullOrWhiteSpace(request.Company))
                    validationErrors.Add("Company is required.");

                if (string.IsNullOrWhiteSpace(request.MotherCompany))
                    validationErrors.Add("MotherCompany is required.");

                if (string.IsNullOrWhiteSpace(request.UserGroup))
                    validationErrors.Add("UserGroup is required.");

                if (!HasCostCentres(request))
                    validationErrors.Add("At least one CostCentre is required.");

                if (request.LvlNo <= 0)
                    validationErrors.Add("LvlNo must be greater than 0.");

                if (validationErrors.Count > 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = string.Join(" ", validationErrors)
                    }));
                }

                var created = _repository.CreateRole(request);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = created,
                        roleId = request.RoleId,
                        message = "Role created successfully."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (InvalidOperationException ex)
            {
                logger.Warn("CreateRole rejected: {error}. request={@request}", ex.Message, request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = ex.Message
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "CreateRole failed. request={@request}", request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot create role.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("{epfNo}/{userType}/update")]
        public IActionResult UpdateRole(string epfNo, string userType, [FromBody] CreateRoleRequest request)
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

                request.OriginalEpfNo = string.IsNullOrWhiteSpace(request.OriginalEpfNo)
                    ? epfNo
                    : request.OriginalEpfNo;
                request.OriginalUserType = string.IsNullOrWhiteSpace(request.OriginalUserType)
                    ? userType
                    : request.OriginalUserType;

                request.RoleId = NormalizeUpper(request.RoleId);
                request.UserType = NormalizeUpper(request.UserType);
                request.OriginalUserType = NormalizeUpper(request.OriginalUserType);

                var validationErrors = new List<string>();

                if (string.IsNullOrWhiteSpace(request.OriginalEpfNo))
                    validationErrors.Add("OriginalEpfNo is required.");

                if (string.IsNullOrWhiteSpace(request.OriginalUserType))
                    validationErrors.Add("OriginalUserType is required.");

                if (string.IsNullOrWhiteSpace(request.EpfNo))
                    validationErrors.Add("EpfNo is required.");

                if (string.IsNullOrWhiteSpace(request.RoleId))
                    validationErrors.Add("RoleId is required.");

                if (string.IsNullOrWhiteSpace(request.UserType))
                    validationErrors.Add("UserType is required.");

                if (string.IsNullOrWhiteSpace(request.Company))
                    validationErrors.Add("Company is required.");

                if (string.IsNullOrWhiteSpace(request.MotherCompany))
                    validationErrors.Add("MotherCompany is required.");

                if (string.IsNullOrWhiteSpace(request.UserGroup))
                    validationErrors.Add("UserGroup is required.");

                if (!HasCostCentres(request))
                    validationErrors.Add("At least one CostCentre is required.");

                if (request.LvlNo <= 0)
                    validationErrors.Add("LvlNo must be greater than 0.");

                if (validationErrors.Count > 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = string.Join(" ", validationErrors)
                    }));
                }

                var updated = _repository.UpdateRole(request);

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = updated,
                        roleId = request.RoleId,
                        message = updated ? "Role updated successfully." : "Role not found."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (InvalidOperationException ex)
            {
                logger.Warn("UpdateRole rejected: {error}. epfNo={epfNo}, userType={userType}, request={@request}", ex.Message, epfNo, userType, request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = ex.Message
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "UpdateRole failed. epfNo={epfNo}, userType={userType}, request={@request}", epfNo, userType, request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot update role.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("{epfNo}/{userType}/costcentres")]
        public IActionResult AddCostCentresToRole(string epfNo, string userType, [FromBody] AddRoleCostCentresRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(epfNo))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "EpfNo is required."
                    }));
                }

                if (request?.CostCentres == null || request.CostCentres.Count == 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "At least one CostCentre is required."
                    }));
                }

                var addedCount = _repository.AddCostCentresToRole(epfNo.Trim(), userType.Trim(), request.CostCentres);

                if (addedCount < 0)
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Role not found."
                    }));
                }

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = true,
                        addedCount,
                        epfNo = epfNo.Trim(),
                        message = addedCount > 0
                            ? "Cost centres added successfully."
                            : "No new cost centres were added."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "AddCostCentresToRole failed. epfNo={epfNo}, userType={userType}, request={@request}", epfNo, userType, request);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot add cost centres.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpPost]
        [Route("{epfNo}/{userType}/delete")]
        public IActionResult DeleteRole(string epfNo, string userType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(epfNo))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "EpfNo is required."
                    }));
                }

                if (string.IsNullOrWhiteSpace(userType))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "UserType is required."
                    }));
                }

                var deleted = _repository.DeleteRole(epfNo.Trim(), userType.Trim());

                return Ok(JObject.FromObject(new
                {
                    data = new
                    {
                        success = deleted,
                        epfNo = epfNo.Trim(),
                        message = deleted ? "Role deleted successfully." : "Role not found."
                    },
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "DeleteRole failed. epfNo={epfNo}, userType={userType}", epfNo, userType);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot delete role.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("companies")]
        public IActionResult GetMotherCompanies()
        {
            try
            {
                var result = _repository.GetMotherCompanies();

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetMotherCompanies failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get mother companies.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("companies/{companyId}/departments")]
        public IActionResult GetDepartmentsByCompany(string companyId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Company is required."
                    }));
                }

                var result = _repository.GetDepartmentsByCompany(companyId.Trim());

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetDepartmentsByCompany failed. companyId={companyId}", companyId);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get departments.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("companies/{companyId}/costcentres")]
        public IActionResult GetCostCentresByCompany(string companyId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(companyId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Company is required."
                    }));
                }

                var result = _repository.GetCostCentresByCompany(companyId.Trim());

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetCostCentresByCompany failed. companyId={companyId}", companyId);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get cost centres.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("departments/{departmentId}/costcentres")]
        public IActionResult GetCostCentresByDepartment(string departmentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(departmentId))
                {
                    return Ok(JObject.FromObject(new
                    {
                        data = (object)null,
                        errorMessage = "Department is required."
                    }));
                }

                var result = _repository.GetCostCentresByDepartment(departmentId.Trim());

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetCostCentresByDepartment failed. departmentId={departmentId}", departmentId);
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get cost centres.",
                    errorDetails = ex.Message
                }));
            }
        }

        [HttpGet]
        [Route("usergroups")]
        public IActionResult GetUserGroups()
        {
            try
            {
                var result = _repository.GetUserGroups();

                return Ok(JObject.FromObject(new
                {
                    data = result,
                    errorMessage = (string)null
                }));
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetUserGroups failed");
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = "Cannot get user groups.",
                    errorDetails = ex.Message
                }));
            }
        }
    }
}