using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace MISReports_Api.DAL
{
    public class RoleReportRepository
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private OracleConnection OpenConnection()
        {
            var conn = new OracleConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private int GetNextRoleRepIdNo(OracleConnection conn)
        {
            const string sql = @"
                SELECT NVL(MAX(REPID_NO), 0) + 1
                FROM REP_ROLES_REP_NEW";

            using (var cmd = new OracleCommand(sql, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // 1) Get reports by category list where favorite = 1
        public List<CategoryReportRecord> GetReportsByCategories(IEnumerable<string> catCodes)
        {
            var codes = (catCodes ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0)
            {
                return new List<CategoryReportRecord>();
            }

            var bindNames = new List<string>();
            for (var i = 0; i < codes.Count; i++)
            {
                bindNames.Add($":cat{i}");
            }

            var sql = $@"
                SELECT r.catcode, c.catname, r.repid, r.repname
                FROM rep_reports_new r
                LEFT JOIN rep_cats_new c ON c.catcode = r.catcode
                WHERE r.catcode IN ({string.Join(",", bindNames)})
                AND r.favorite = 1
                ORDER BY r.catcode, r.repid";

            var results = new List<CategoryReportRecord>();

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;

                for (var i = 0; i < codes.Count; i++)
                {
                    cmd.Parameters.Add($"cat{i}", OracleDbType.Varchar2).Value = codes[i];
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new CategoryReportRecord
                        {
                            CatCode = reader["CATCODE"]?.ToString(),
                            CatName = reader["CATNAME"]?.ToString(),
                            RepId = reader["REPID"]?.ToString(),
                            RepName = reader["REPNAME"]?.ToString()
                        });
                    }
                }
            }

            return results;
        }

        // 9) Alternative clean query (single category)
        public List<CategoryReportFullRecord> GetReportsByCategory(string catCode)
        {
            const string sql = @"
                SELECT r.catcode, c.catname, r.repid, r.repname, r.favorite
                FROM rep_reports_new r
                LEFT JOIN rep_cats_new c ON c.catcode = r.catcode
                WHERE r.catcode = :catCode
                AND r.favorite = 1";

            var results = new List<CategoryReportFullRecord>();

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("catCode", OracleDbType.Varchar2).Value = catCode?.Trim();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new CategoryReportFullRecord
                        {
                            CatCode = reader["CATCODE"]?.ToString(),
                            CatName = reader["CATNAME"]?.ToString(),
                            RepId = reader["REPID"]?.ToString(),
                            RepName = reader["REPNAME"]?.ToString(),
                            Favorite = reader["FAVORITE"]?.ToString()
                        });
                    }
                }
            }

            return results;
        }

        // 2) Insert report to user role
        public int InsertRoleReport(string roleId, string catCode, string repId, string favorite)
        {
            const string sql = @"
                INSERT INTO rep_roles_rep_new (repid_no, roleid, catcode, repid, favorite)
                VALUES (:repIdNo, :roleId, :catCode, :repId, :favorite)";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("repIdNo", OracleDbType.Int32).Value = GetNextRoleRepIdNo(conn);
                cmd.Parameters.Add("roleId", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("catCode", OracleDbType.Varchar2).Value = catCode?.Trim();
                cmd.Parameters.Add("repId", OracleDbType.Varchar2).Value = repId?.Trim();
                cmd.Parameters.Add("favorite", OracleDbType.Varchar2).Value = string.IsNullOrWhiteSpace(favorite) ? "1" : favorite.Trim();

                return cmd.ExecuteNonQuery();
            }
        }

        // 3) Check existing record before insert
        public int GetExistingCount(string roleId, string catCode, string repId)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM rep_roles_rep_new
                WHERE roleid = :roleId
                AND catcode = :catCode
                AND repid = :repId";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleId", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("catCode", OracleDbType.Varchar2).Value = catCode?.Trim();
                cmd.Parameters.Add("repId", OracleDbType.Varchar2).Value = repId?.Trim();

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // SAVE_USERROLEREPS behavior: avoid duplicate inserts
        public int SaveUserRoleReports(string roleId, IEnumerable<RoleReportItemRequest> reports)
        {
            var items = (reports ?? Enumerable.Empty<RoleReportItemRequest>())
                .Where(x => x != null
                    && !string.IsNullOrWhiteSpace(x.CatCode)
                    && !string.IsNullOrWhiteSpace(x.RepId))
                .ToList();

            var insertedRows = 0;
            foreach (var item in items)
            {
                var exists = GetExistingCount(roleId, item.CatCode, item.RepId);
                if (exists == 0)
                {
                    insertedRows += InsertRoleReport(roleId, item.CatCode, item.RepId, item.Favorite);
                }
            }

            return insertedRows;
        }

        // 4) Delete all reports for user
        public int DeleteAllReports(string roleId)
        {
            const string sql = @"
                DELETE FROM rep_roles_rep_new
                WHERE roleid = :roleId";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleId", OracleDbType.Varchar2).Value = roleId?.Trim();
                return cmd.ExecuteNonQuery();
            }
        }

        // 5) Delete reports by category
        public int DeleteReportsByCategory(string roleId, IEnumerable<string> catCodes)
        {
            var codes = (catCodes ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count == 0)
            {
                return 0;
            }

            var bindNames = new List<string>();
            for (var i = 0; i < codes.Count; i++)
            {
                bindNames.Add($":cat{i}");
            }

            var sql = $@"
                DELETE FROM rep_roles_rep_new
                WHERE roleid = :roleId
                AND catcode IN ({string.Join(",", bindNames)})";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleId", OracleDbType.Varchar2).Value = roleId?.Trim();

                for (var i = 0; i < codes.Count; i++)
                {
                    cmd.Parameters.Add($"cat{i}", OracleDbType.Varchar2).Value = codes[i];
                }

                return cmd.ExecuteNonQuery();
            }
        }

        // 6) Delete report by name
        public int DeleteReportByName(string roleId, string repId)
        {
            const string sql = @"
                DELETE FROM rep_roles_rep_new
                WHERE roleid = :roleId
                AND repid = :repId";

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleId", OracleDbType.Varchar2).Value = roleId?.Trim();
                cmd.Parameters.Add("repId", OracleDbType.Varchar2).Value = repId?.Trim();
                return cmd.ExecuteNonQuery();
            }
        }

        // 7) Load all reports for display
        public List<RoleReportRecord> GetAllRoleReports()
        {
            const string sql = @"
                SELECT r.roleid, r.catcode, c.catname, r.repid, p.repname, r.favorite
                FROM rep_roles_rep_new r
                LEFT JOIN rep_cats_new c ON c.catcode = r.catcode
                LEFT JOIN rep_reports_new p ON p.repid = r.repid
                ORDER BY r.roleid, r.catcode, r.repid";

            var results = new List<RoleReportRecord>();

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(new RoleReportRecord
                    {
                        RoleId = reader["ROLEID"]?.ToString(),
                        CatCode = reader["CATCODE"]?.ToString(),
                        CatName = reader["CATNAME"]?.ToString(),
                        RepId = reader["REPID"]?.ToString(),
                        RepName = reader["REPNAME"]?.ToString(),
                        Favorite = reader["FAVORITE"]?.ToString()
                    });
                }
            }

            return results;
        }

        // 8) Load reports with category details (JOIN)
        public List<RoleReportWithCategoryRecord> GetAllRoleReportsWithCategory()
        {
            const string sql = @"
                SELECT r.roleid, r.catcode, c.catname, r.repid, p.repname
                FROM rep_roles_rep_new r
                LEFT JOIN rep_cats_new c ON c.catcode = r.catcode
                LEFT JOIN rep_reports_new p ON p.repid = r.repid
                ORDER BY r.roleid, r.catcode, r.repid";

            var results = new List<RoleReportWithCategoryRecord>();

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(new RoleReportWithCategoryRecord
                    {
                        RoleId = reader["ROLEID"]?.ToString(),
                        CatCode = reader["CATCODE"]?.ToString(),
                        CatName = reader["CATNAME"]?.ToString(),
                        RepId = reader["REPID"]?.ToString(),
                        RepName = reader["REPNAME"]?.ToString()
                    });
                }
            }

            return results;
        }

        // 10) Get reports for specific user
        public List<RoleReportRecord> GetReportsByUser(string roleId)
        {
            const string sql = @"
                SELECT r.roleid, r.catcode, c.catname, r.repid, p.repname, r.favorite
                FROM rep_roles_rep_new r
                LEFT JOIN rep_cats_new c ON c.catcode = r.catcode
                LEFT JOIN rep_reports_new p ON p.repid = r.repid
                WHERE r.roleid = :roleId
                ORDER BY r.catcode, r.repid";

            var results = new List<RoleReportRecord>();

            using (var conn = OpenConnection())
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleId", OracleDbType.Varchar2).Value = roleId?.Trim();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new RoleReportRecord
                        {
                            RoleId = reader["ROLEID"]?.ToString(),
                            CatCode = reader["CATCODE"]?.ToString(),
                            CatName = reader["CATNAME"]?.ToString(),
                            RepId = reader["REPID"]?.ToString(),
                            RepName = reader["REPNAME"]?.ToString(),
                            Favorite = reader["FAVORITE"]?.ToString()
                        });
                    }
                }
            }

            return results;
        }
    }

    public class CategoryReportRecord
    {
        public string CatCode { get; set; }
        public string CatName { get; set; }
        public string RepId { get; set; }
        public string RepName { get; set; }
    }

    public class CategoryReportFullRecord : CategoryReportRecord
    {
        public string Favorite { get; set; }
    }

    public class RoleReportRecord
    {
        public string RoleId { get; set; }
        public string CatCode { get; set; }
        public string CatName { get; set; }
        public string RepId { get; set; }
        public string RepName { get; set; }
        public string Favorite { get; set; }
    }

    public class RoleReportWithCategoryRecord
    {
        public string RoleId { get; set; }
        public string CatCode { get; set; }
        public string CatName { get; set; }
        public string RepId { get; set; }
        public string RepName { get; set; }
    }
}
