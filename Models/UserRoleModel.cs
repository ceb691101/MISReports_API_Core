using System;

namespace MISReports_Api.Models
{
    public class UserRoleModel
    {
        public string RoleId { get; set; }
        public string UserGroup { get; set; }
        public string USERTYPE { get; set; }
        public string COMPANY { get; set; }
        public string BillMap { get; set; }
        public string LevelNo { get; set; }
    }
}