// Models/Admin/RepRoles/CreateRoleRequest.cs
using System.Collections.Generic;

namespace MISReports_Api.Models
{
    public class CreateRoleRequest
    {
        public string OriginalEpfNo { get; set; }
        public string OriginalUserType { get; set; }
        public string EpfNo { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public string UserType { get; set; }
        public string Company { get; set; }
        public string MotherCompany { get; set; }
        public string UserGroup { get; set; }
        public string CostCentre { get; set; }
        public List<string> CostCentres { get; set; }
        public int LvlNo { get; set; }
        public string AddUser { get; set; }
        public string UpdateUser { get; set; }
    }

    public class AddRoleCostCentresRequest
    {
        public List<string> CostCentres { get; set; }
    }
}