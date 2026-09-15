namespace MISReports_Api.Models
{
    public class MotherCompanyOptionModel
    {
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
    }

    public class CostCentreOptionModel
    {
        public string CostCentreId { get; set; }
        public string CostCentreName { get; set; }
        public string CostCentreDisplay { get; set; } // Formatted as "ID:Name"
    }

    public class UserGroupOptionModel
    {
        public string UserGroupId { get; set; }
        public string UserGroupName { get; set; }
    }

    public class DepartmentOptionModel
    {
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
    }
}