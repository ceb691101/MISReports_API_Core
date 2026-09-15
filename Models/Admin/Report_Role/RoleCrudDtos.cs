using System.Collections.Generic;

namespace MISReports_Api.Models
{
	public class RoleCategoryDto
	{
		public string CatCode { get; set; }
		public string CatName { get; set; }
	}

	public class RoleReportLookupDto
	{
		public string CatCode { get; set; }
		public string RepId { get; set; }
		public string RepName { get; set; }
	}

	public class RoleAssignedReportDto
	{
		public string RoleId { get; set; }
		public string CatCode { get; set; }
		public string RepId { get; set; }
	}

	public class RoleReportItemRequest
	{
		public string CatCode { get; set; }
		public string RepId { get; set; }
		public string Favorite { get; set; }
	}

	public class SaveUserRoleReportsRequest
	{
		public string RoleId { get; set; }
		public string AddReports { get; set; }
		public List<string> CatCodes { get; set; }
		public List<RoleReportItemRequest> Reports { get; set; }
	}

	public class ReportsByCategoryRequest
	{
		public string AddReports { get; set; }
		public List<string> CatCodes { get; set; }
	}

	public class RoleSaveResultDto
	{
		public int Inserted { get; set; }
		public int Updated { get; set; }
		public int Ignored { get; set; }
	}
}
