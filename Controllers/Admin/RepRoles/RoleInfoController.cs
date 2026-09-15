using MISReports_Api.DAL;
using MISReports_Api.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace MISReports_Api.Controllers
{
    [RoutePrefix("api/roleinfo")]
    public class RoleInfoController : ApiController
    {
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
        public IHttpActionResult GetAdminRoles()
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
        public IHttpActionResult GetUserRoles()
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
        public IHttpActionResult CreateRole([FromBody] CreateRoleRequest request)
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
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = ex.Message
                }));
            }
            catch (Exception ex)
            {
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
        public IHttpActionResult UpdateRole(string epfNo, string userType, [FromBody] CreateRoleRequest request)
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
                return Ok(JObject.FromObject(new
                {
                    data = (object)null,
                    errorMessage = ex.Message
                }));
            }
            catch (Exception ex)
            {
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
        public IHttpActionResult AddCostCentresToRole(string epfNo, string userType, [FromBody] AddRoleCostCentresRequest request)
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
        public IHttpActionResult DeleteRole(string epfNo, string userType)
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
        public IHttpActionResult GetMotherCompanies()
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
        public IHttpActionResult GetDepartmentsByCompany(string companyId)
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
        public IHttpActionResult GetCostCentresByCompany(string companyId)
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
        public IHttpActionResult GetCostCentresByDepartment(string departmentId)
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
        public IHttpActionResult GetUserGroups()
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