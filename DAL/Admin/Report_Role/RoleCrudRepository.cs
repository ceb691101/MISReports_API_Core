using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;

namespace MISReports_Api.DAL
{
	public class RoleCrudRepository
	{
		private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

		private static string Normalize(string value)
		{
			return value?.Trim();
		}

		private static string NormalizeKey(string value)
		{
			var trimmed = value?.Trim();
			return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed.ToUpperInvariant();
		}

		private static List<string> NormalizeDistinctCodes(IEnumerable<string> codes)
		{
			return (codes ?? Enumerable.Empty<string>())
				.Where(code => !string.IsNullOrWhiteSpace(code))
				.Select(code => NormalizeKey(code))
				.Where(code => !string.IsNullOrWhiteSpace(code))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		public List<RoleReportLookupDto> GetReportsByCategory(List<string> catCodes)
		{
			var normalizedCodes = NormalizeDistinctCodes(catCodes);
			var result = new List<RoleReportLookupDto>();

			if (normalizedCodes.Count == 0)
			{
				return result;
			}

			try
			{
				using (var conn = new OracleConnection(connectionString))
				{
					conn.Open();

					var bindNames = new List<string>();
					for (var i = 0; i < normalizedCodes.Count; i++)
					{
						bindNames.Add(":cat" + i);
					}

					var sql = @"
SELECT CATCODE, REPID
FROM REP_REPORTS_NEW
WHERE UPPER(TRIM(CATCODE)) IN (" + string.Join(",", bindNames) + @")
AND FAVORITE = '1'
AND ACTIVE = '1'";

					using (var cmd = new OracleCommand(sql, conn))
					{
						cmd.BindByName = true;
						for (var i = 0; i < normalizedCodes.Count; i++)
						{
							cmd.Parameters.Add("cat" + i, OracleDbType.Varchar2).Value = normalizedCodes[i];
						}

						using (var reader = cmd.ExecuteReader())
						{
							while (reader.Read())
							{
								result.Add(new RoleReportLookupDto
								{
									CatCode = reader["CATCODE"]?.ToString()?.Trim(),
									RepId = reader["REPID"]?.ToString()?.Trim(),
									RepName = string.Empty
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error in GetReportsByCategory: " + ex.Message);
				throw;
			}

			return result;
		}

		public bool ExistsRoleReportExact(string roleId, string repId, string catCode)
		{
			const string sql = @"
SELECT CATCODE, REPID
FROM REP_ROLES_REP_NEW
WHERE UPPER(TRIM(ROLEID)) = :ROLEID
AND UPPER(TRIM(REPID)) = :REPID
AND UPPER(TRIM(CATCODE)) = :CATCODE";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = NormalizeKey(roleId);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = NormalizeKey(repId);
					cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = NormalizeKey(catCode);

					using (var reader = cmd.ExecuteReader())
					{
						return reader.Read();
					}
				}
			}
		}

		private bool ExistsRoleReportByRepId(string roleId, string repId)
		{
			const string sql = @"
SELECT 1
FROM REP_ROLES_REP_NEW
WHERE UPPER(TRIM(ROLEID)) = :ROLEID
AND UPPER(TRIM(REPID)) = :REPID";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = NormalizeKey(roleId);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = NormalizeKey(repId);
					using (var reader = cmd.ExecuteReader())
					{
						return reader.Read();
					}
				}
			}
		}

		private int GetReportEntryRepIdNo(OracleConnection conn, string catCode, string repId)
		{
			const string sql = @"
SELECT REPID_NO
FROM REP_REPORTS_NEW
WHERE UPPER(TRIM(CATCODE)) = :CATCODE
AND UPPER(TRIM(REPID)) = :REPID";

			using (var cmd = new OracleCommand(sql, conn))
			{
				cmd.BindByName = true;
				cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = NormalizeKey(catCode);
				cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = NormalizeKey(repId);

				var repIdNo = cmd.ExecuteScalar();
				if (repIdNo == null || repIdNo == DBNull.Value)
				{
					throw new InvalidOperationException("Cannot find REPID_NO in REP_REPORTS_NEW for the given CATCODE and REPID.");
				}

				return Convert.ToInt32(repIdNo);
			}
		}

		public int InsertUserReport(string roleId, string catCode, string repId)
		{
			const string sql = @"
INSERT INTO REP_ROLES_REP_NEW
(REPID_NO, ROLEID, CATCODE, REPID, FAVORITE)
VALUES (:REPID_NO, :ROLEID, :CATCODE, :REPID, '1')";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				var repIdNo = GetReportEntryRepIdNo(conn, catCode, repId);
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("REPID_NO", OracleDbType.Int32).Value = repIdNo;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = NormalizeKey(roleId);
					cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = NormalizeKey(catCode);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = NormalizeKey(repId);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public int UpdateUserReport(string roleId, string catCode, string repId)
		{
			const string sql = @"
UPDATE REP_ROLES_REP_NEW
SET REPID_NO = :REPID_NO,
	CATCODE = :CATCODE,
	REPID   = :REPID
WHERE UPPER(TRIM(ROLEID)) = :ROLEID
AND UPPER(TRIM(REPID)) = :REPID";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				var repIdNo = GetReportEntryRepIdNo(conn, catCode, repId);
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("REPID_NO", OracleDbType.Int32).Value = repIdNo;
					cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = NormalizeKey(catCode);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = NormalizeKey(repId);
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = NormalizeKey(roleId);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public RoleSaveResultDto SaveUserRoleReports(string roleId, List<RoleReportItemRequest> reports)
		{
			var result = new RoleSaveResultDto();

			if (string.IsNullOrWhiteSpace(roleId) || reports == null || reports.Count == 0)
			{
				return result;
			}

			foreach (var item in reports)
			{
				if (item == null || string.IsNullOrWhiteSpace(item.RepId) || string.IsNullOrWhiteSpace(item.CatCode))
				{
					result.Ignored++;
					continue;
				}

				var role = Normalize(roleId);
				var rep = Normalize(item.RepId);
				var cat = Normalize(item.CatCode);

				if (ExistsRoleReportExact(role, rep, cat))
				{
					// Keep servlet-compatible behavior: update when existing.
					UpdateUserReport(role, cat, rep);
					result.Updated++;
					continue;
				}

				if (ExistsRoleReportByRepId(role, rep))
				{
					UpdateUserReport(role, cat, rep);
					result.Updated++;
					continue;
				}

				InsertUserReport(role, cat, rep);
				result.Inserted++;
			}

			return result;
		}

		public int DeleteAllReports(string roleId)
		{
			const string sql = @"
DELETE FROM REP_ROLES_REP_NEW
WHERE UPPER(TRIM(ROLEID)) = :ROLEID";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = NormalizeKey(roleId);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public int DeleteReportsByCategory(string roleId, string catCode)
		{
			const string sql = @"
DELETE FROM REP_ROLES_REP_NEW
WHERE UPPER(TRIM(ROLEID)) = :ROLEID
AND UPPER(TRIM(CATCODE)) = :CATCODE";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = NormalizeKey(roleId);
					cmd.Parameters.Add("CATCODE", OracleDbType.Varchar2).Value = NormalizeKey(catCode);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public int DeleteReportByName(string roleId, string repId)
		{
			const string sql = @"
DELETE FROM REP_ROLES_REP_NEW
WHERE UPPER(TRIM(ROLEID)) = :ROLEID
AND UPPER(TRIM(REPID)) = :REPID";

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = NormalizeKey(roleId);
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = NormalizeKey(repId);
					return cmd.ExecuteNonQuery();
				}
			}
		}

		public List<RoleReportLookupDto> GetReportsByName(string repId)
		{
			const string sql = @"
SELECT CATCODE, REPID, REPNAME
FROM REP_REPORTS_NEW
WHERE UPPER(TRIM(REPID)) = :REPID";

			var result = new List<RoleReportLookupDto>();

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("REPID", OracleDbType.Varchar2).Value = NormalizeKey(repId);

					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							result.Add(new RoleReportLookupDto
							{
								CatCode = reader["CATCODE"]?.ToString()?.Trim(),
								RepId = reader["REPID"]?.ToString()?.Trim(),
								RepName = reader["REPNAME"]?.ToString()?.Trim()
							});
						}
					}
				}
			}

			return result;
		}

		public List<RoleCategoryDto> GetAllCategories()
		{
			const string sql = @"
SELECT CATCODE, CATNAME
FROM REP_CATS_NEW";

			var result = new List<RoleCategoryDto>();

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						result.Add(new RoleCategoryDto
						{
							CatCode = reader["CATCODE"]?.ToString()?.Trim(),
							CatName = reader["CATNAME"]?.ToString()?.Trim()
						});
					}
				}
			}

			return result;
		}

		public List<RoleReportLookupDto> GetAllActiveReports()
		{
			const string sql = @"
SELECT CATCODE, REPID, REPNAME
FROM REP_REPORTS_NEW
WHERE ACTIVE = '1'";

			var result = new List<RoleReportLookupDto>();

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				using (var reader = cmd.ExecuteReader())
				{
					while (reader.Read())
					{
						result.Add(new RoleReportLookupDto
						{
							CatCode = reader["CATCODE"]?.ToString()?.Trim(),
							RepId = reader["REPID"]?.ToString()?.Trim(),
							RepName = reader["REPNAME"]?.ToString()?.Trim()
						});
					}
				}
			}

			return result;
		}

		public List<RoleAssignedReportDto> GetUserAssignedReports(string roleId)
		{
			const string sql = @"
SELECT ROLEID, CATCODE, REPID
FROM REP_ROLES_REP_NEW
WHERE UPPER(TRIM(ROLEID)) = :ROLEID";

			var result = new List<RoleAssignedReportDto>();

			using (var conn = new OracleConnection(connectionString))
			{
				conn.Open();
				using (var cmd = new OracleCommand(sql, conn))
				{
					cmd.BindByName = true;
					cmd.Parameters.Add("ROLEID", OracleDbType.Varchar2).Value = NormalizeKey(roleId);

					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							result.Add(new RoleAssignedReportDto
							{
								RoleId = reader["ROLEID"]?.ToString()?.Trim(),
								CatCode = reader["CATCODE"]?.ToString()?.Trim(),
								RepId = reader["REPID"]?.ToString()?.Trim()
							});
						}
					}
				}
			}
		return result;
		}
	}
}
