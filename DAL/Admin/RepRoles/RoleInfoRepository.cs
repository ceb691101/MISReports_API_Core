using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class RoleInfoRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private List<string> NormalizeCostCentres(CreateRoleRequest request)
        {
            var normalized = new List<string>();

            if (request?.CostCentres != null)
            {
                foreach (var costCentre in request.CostCentres)
                {
                    var trimmed = costCentre?.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed) && !normalized.Contains(trimmed))
                    {
                        normalized.Add(trimmed);
                    }
                }
            }

            var singleCostCentre = request?.CostCentre?.Trim();
            if (!string.IsNullOrWhiteSpace(singleCostCentre) && !normalized.Contains(singleCostCentre))
            {
                normalized.Add(singleCostCentre);
            }

            return normalized;
        }

        private List<string> NormalizeCostCentres(IEnumerable<string> costCentres)
        {
            var normalized = new List<string>();

            if (costCentres == null)
            {
                return normalized;
            }

            foreach (var costCentre in costCentres)
            {
                var trimmed = costCentre?.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) && !normalized.Contains(trimmed))
                {
                    normalized.Add(trimmed);
                }
            }

            return normalized;
        }

        private int GetCostCentreLvlNo(OracleConnection conn, OracleTransaction transaction, string costCentreId)
        {
            try
            {
                const string sql = @"
                    SELECT lvl_no
                    FROM glcompm
                    WHERE TRIM(comp_id) = :cost_centre_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Transaction = transaction;
                    cmd.BindByName = true;
                    cmd.Parameters.Add("cost_centre_id", OracleDbType.Varchar2).Value = costCentreId?.Trim();

                    var result = cmd.ExecuteScalar();
                    if (result != null && int.TryParse(result.ToString(), out var lvlNo))
                    {
                        return lvlNo;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCostCentreLvlNo: {ex.Message}");
            }

            return 0; // Default value if not found
        }

        private string NormalizeUserType(string userType)
        {
            var t = userType?.Trim();
            if (string.IsNullOrWhiteSpace(t))
            {
                return string.Empty;
            }

            if (string.Equals(t, "ADMINISTRATOR", StringComparison.OrdinalIgnoreCase))
            {
                return "ADMIN";
            }

            if (string.Equals(t, "USER", StringComparison.OrdinalIgnoreCase))
            {
                return "USER";
            }

            return t.ToUpperInvariant();
        }

        private static string NormalizeRoleId(string roleId)
        {
            return string.IsNullOrWhiteSpace(roleId)
                ? string.Empty
                : roleId.Trim().ToUpperInvariant();
        }

        private static string NormalizeRoleUserType(string userType)
        {
            var t = userType?.Trim();
            if (string.IsNullOrWhiteSpace(t))
            {
                return string.Empty;
            }

            if (string.Equals(t, "ADMINISTRATOR", StringComparison.OrdinalIgnoreCase))
            {
                return "ADMIN";
            }

            if (string.Equals(t, "USER", StringComparison.OrdinalIgnoreCase))
            {
                return "USER";
            }

            return t.ToUpperInvariant();
        }

        public List<RoleInfoModel> GetAdminRoles()
        {
            return GetRolesByUserType("ADMIN%");
        }

        public List<RoleInfoModel> GetUserRoles()
        {
            return GetRolesByUserType("USER%");
        }

        private List<string> GetCostCentresByRoleId(OracleConnection conn, string roleId)
        {
            var costCentres = new List<string>();

            const string sql = @"
                SELECT TRIM(COSTCENTRE) AS COSTCENTRE
                FROM REP_ROLES_CCT_NEW
                WHERE TRIM(ROLEID) = :roleId
                ORDER BY COSTCENTRE";

            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("roleId", OracleDbType.Varchar2).Value = roleId?.Trim();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var costCentre = reader["COSTCENTRE"]?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(costCentre))
                        {
                            costCentres.Add(costCentre);
                        }
                    }
                }
            }

            return costCentres;
        }

        private List<RoleInfoModel> GetRolesByUserType(string userTypePattern)
        {
            var roles = new List<RoleInfoModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"SELECT r.EPF_NO,
                                          r.roleid,
                                          r.rolename,
                                          r.company,
                                          r.mcompany,
                                          r.usertype,
                                          r.user_group
                                   FROM REP_ROLE_NEW r
                                   WHERE UPPER(TRIM(r.usertype)) LIKE :userTypePattern
                                   ORDER BY r.roleid";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("userTypePattern", OracleDbType.Varchar2).Value = userTypePattern;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var roleId = reader["ROLEID"]?.ToString();
                                var costCentres = GetCostCentresByRoleId(conn, roleId);
                                var costCentreValue = costCentres.Count > 0
                                    ? string.Join(",", costCentres)
                                    : string.Empty;

                                roles.Add(new RoleInfoModel
                                {
                                    EpfNo = reader["EPF_NO"]?.ToString(),
                                    RoleId = roleId,
                                    RoleName = reader["ROLENAME"]?.ToString(),
                                    Company = reader["COMPANY"]?.ToString(),
                                    MotherCompany = reader["MCOMPANY"]?.ToString(),
                                    UserGroup = reader["USER_GROUP"]?.ToString(),
                                    CostCentre = costCentreValue,
                                    CostCentres = costCentres,
                                    UserType = NormalizeUserType(reader["USERTYPE"]?.ToString())
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetRolesByUserType: {ex.Message}");
                throw;
            }

            return roles;
        }
        public bool CreateRole(CreateRoleRequest request)
        {
            var costCentres = NormalizeCostCentres(request);
            var normalizedRoleId = NormalizeRoleId(request?.RoleId);
            var normalizedUserType = NormalizeUserType(request?.UserType);

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string checkRoleSql = @"
                            SELECT COUNT(1)
                            FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :epf_no
                              AND CASE WHEN UPPER(TRIM(USERTYPE)) = 'ADMINISTRATOR' THEN 'ADMIN' ELSE UPPER(TRIM(USERTYPE)) END = :user_type";

                        using (var checkCmd = new OracleCommand(checkRoleSql, conn))
                        {
                            checkCmd.Transaction = transaction;
                            checkCmd.BindByName = true;
                            checkCmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = request.EpfNo?.Trim();
                            checkCmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = NormalizeRoleUserType(request.UserType);

                            var roleCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (roleCount > 0)
                            {
                                transaction.Rollback();
                                throw new InvalidOperationException("A role mapping with the same EPF Number and User Type already exists.");
                            }
                        }

                        const string insertRoleSql = @"
                            INSERT INTO REP_ROLE_NEW
                            (
                                EPF_NO,
                                ROLEID,
                                ROLENAME,
                                USERTYPE,
                                COMPANY,
                                MCOMPANY,
                                USER_GROUP,
                                ADD_USER,
                                ADD_DATE
                            )
                            VALUES
                            (
                                :epf_no,
                                :role_id,
                                :role_name,
                                :user_type,
                                :company,
                                :mcompany,
                                :user_group,
                                :add_user,
                                SYSDATE
                            )";

                        using (var cmd = new OracleCommand(insertRoleSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;

                            cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = request.EpfNo?.Trim();
                            cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = normalizedRoleId;
                            cmd.Parameters.Add("role_name", OracleDbType.Varchar2).Value = (object)request.RoleName?.Trim() ?? DBNull.Value;
                            cmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = normalizedUserType;
                            cmd.Parameters.Add("company", OracleDbType.Varchar2).Value = request.Company?.Trim();
                            cmd.Parameters.Add("mcompany", OracleDbType.Varchar2).Value = request.MotherCompany?.Trim();
                            cmd.Parameters.Add("user_group", OracleDbType.Varchar2).Value = request.UserGroup?.Trim();
                            cmd.Parameters.Add("add_user", OracleDbType.Varchar2).Value = (object)request.AddUser?.Trim() ?? DBNull.Value;

                            cmd.ExecuteNonQuery();
                        }

                        const string checkRoleCctSql = @"
                            SELECT COUNT(1)
                            FROM REP_ROLES_CCT_NEW
                            WHERE TRIM(ROLEID) = :role_id
                              AND TRIM(COSTCENTRE) = :costcentre";

                        const string insertRoleCctSql = @"
                            INSERT INTO REP_ROLES_CCT_NEW
                            (
                                ROLEID,
                                COSTCENTRE,
                                LVL_NO,
                                STATUS
                            )
                            VALUES
                            (
                                :role_id,
                                :costcentre,
                                :lvl_no,
                                2
                            )";

                        foreach (var costCentre in costCentres)
                        {
                            bool cctExists = false;
                            using (var checkCmd = new OracleCommand(checkRoleCctSql, conn))
                            {
                                checkCmd.Transaction = transaction;
                                checkCmd.BindByName = true;
                                checkCmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = normalizedRoleId;
                                checkCmd.Parameters.Add("costcentre", OracleDbType.Varchar2).Value = costCentre;
                                cctExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                            }

                            if (cctExists)
                            {
                                continue;
                            }

                            int lvlNo = GetCostCentreLvlNo(conn, transaction, costCentre);

                            using (var cmd = new OracleCommand(insertRoleCctSql, conn))
                            {
                                cmd.Transaction = transaction;
                                cmd.BindByName = true;

                                cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = normalizedRoleId;
                                cmd.Parameters.Add("costcentre", OracleDbType.Varchar2).Value = costCentre;
                                cmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = lvlNo;

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in CreateRole: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public bool UpdateRole(CreateRoleRequest request)
        {
            var costCentres = NormalizeCostCentres(request);
            var normalizedRoleId = NormalizeRoleId(request?.RoleId);
            var normalizedUserType = NormalizeUserType(request?.UserType);
            var originalUserType = NormalizeRoleUserType(request?.OriginalUserType);

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Fetch the original RoleId first to check if it has changed and if the role exists
                        string originalRoleId = null;
                        const string getOriginalRoleIdSql = @"
                            SELECT TRIM(ROLEID)
                            FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :original_epf_no
                              AND CASE WHEN UPPER(TRIM(USERTYPE)) = 'ADMINISTRATOR' THEN 'ADMIN' ELSE UPPER(TRIM(USERTYPE)) END = :original_user_type";

                        using (var getCmd = new OracleCommand(getOriginalRoleIdSql, conn))
                        {
                            getCmd.Transaction = transaction;
                            getCmd.BindByName = true;
                            getCmd.Parameters.Add("original_epf_no", OracleDbType.Varchar2).Value = request.OriginalEpfNo?.Trim();
                            getCmd.Parameters.Add("original_user_type", OracleDbType.Varchar2).Value = originalUserType;

                            var result = getCmd.ExecuteScalar();
                            if (result == null || result == DBNull.Value)
                            {
                                transaction.Rollback();
                                return false;
                            }
                            originalRoleId = result.ToString().Trim();
                        }

                        const string targetRoleSql = @"
                            SELECT COUNT(1)
                            FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :epf_no
                              AND CASE WHEN UPPER(TRIM(USERTYPE)) = 'ADMINISTRATOR' THEN 'ADMIN' ELSE UPPER(TRIM(USERTYPE)) END = :user_type";

                        using (var targetCmd = new OracleCommand(targetRoleSql, conn))
                        {
                            targetCmd.Transaction = transaction;
                            targetCmd.BindByName = true;
                            targetCmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = request.EpfNo?.Trim();
                            targetCmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = normalizedUserType;

                            var targetCount = Convert.ToInt32(targetCmd.ExecuteScalar());
                            var sameCompositeKey = string.Equals(request.OriginalEpfNo?.Trim(), request.EpfNo?.Trim(), StringComparison.OrdinalIgnoreCase)
                                && string.Equals(originalUserType, normalizedUserType, StringComparison.OrdinalIgnoreCase);

                            if (targetCount > 0 && !sameCompositeKey)
                            {
                                transaction.Rollback();
                                throw new InvalidOperationException("A role mapping with the target EPF Number and User Type already exists.");
                            }
                        }

                        const string updateRoleSql = @"
                            UPDATE REP_ROLE_NEW
                            SET EPF_NO = :epf_no,
                                ROLEID = :new_role_id,
                                ROLENAME = :role_name,
                                USERTYPE = :user_type,
                                COMPANY = :company,
                                MCOMPANY = :mcompany,
                                USER_GROUP = :user_group,
                                UPDATE_USER = :update_user,
                                UPDATE_DATE = SYSDATE
                            WHERE TRIM(EPF_NO) = :original_epf_no
                              AND CASE WHEN UPPER(TRIM(USERTYPE)) = 'ADMINISTRATOR' THEN 'ADMIN' ELSE UPPER(TRIM(USERTYPE)) END = :original_user_type";

                        using (var cmd = new OracleCommand(updateRoleSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;

                            cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = request.EpfNo?.Trim();
                            cmd.Parameters.Add("new_role_id", OracleDbType.Varchar2).Value = normalizedRoleId;
                            cmd.Parameters.Add("role_name", OracleDbType.Varchar2).Value = (object)request.RoleName?.Trim() ?? DBNull.Value;
                            cmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = normalizedUserType;
                            cmd.Parameters.Add("company", OracleDbType.Varchar2).Value = request.Company?.Trim();
                            cmd.Parameters.Add("mcompany", OracleDbType.Varchar2).Value = request.MotherCompany?.Trim();
                            cmd.Parameters.Add("user_group", OracleDbType.Varchar2).Value = request.UserGroup?.Trim();
                            cmd.Parameters.Add("update_user", OracleDbType.Varchar2).Value = (object)request.UpdateUser?.Trim() ?? DBNull.Value;
                            cmd.Parameters.Add("original_epf_no", OracleDbType.Varchar2).Value = request.OriginalEpfNo?.Trim();
                            cmd.Parameters.Add("original_user_type", OracleDbType.Varchar2).Value = originalUserType;

                            cmd.ExecuteNonQuery();
                        }

                        // 2. Handle cost centre deletion and addition.
                        bool isRoleIdChanged = !string.Equals(originalRoleId, normalizedRoleId, StringComparison.OrdinalIgnoreCase);

                        if (isRoleIdChanged)
                        {
                            // If RoleId changed, delete old RoleId's cost centres ONLY if no other record is using it
                            const string checkOldRoleIdInUseSql = @"
                                SELECT COUNT(1)
                                FROM REP_ROLE_NEW
                                WHERE TRIM(ROLEID) = :original_role_id";

                            bool oldRoleIdStillInUse = false;
                            using (var checkUseCmd = new OracleCommand(checkOldRoleIdInUseSql, conn))
                            {
                                checkUseCmd.Transaction = transaction;
                                checkUseCmd.BindByName = true;
                                checkUseCmd.Parameters.Add("original_role_id", OracleDbType.Varchar2).Value = originalRoleId;
                                oldRoleIdStillInUse = Convert.ToInt32(checkUseCmd.ExecuteScalar()) > 0;
                            }

                            if (!oldRoleIdStillInUse)
                            {
                                const string deleteOldRoleCctSql = @"
                                    DELETE FROM REP_ROLES_CCT_NEW
                                    WHERE TRIM(ROLEID) = :original_role_id";

                                using (var deleteCmd = new OracleCommand(deleteOldRoleCctSql, conn))
                                {
                                    deleteCmd.Transaction = transaction;
                                    deleteCmd.BindByName = true;
                                    deleteCmd.Parameters.Add("original_role_id", OracleDbType.Varchar2).Value = originalRoleId;
                                    deleteCmd.ExecuteNonQuery();
                                }
                            }

                            // Delete any existing cost centres for the new role ID before inserting
                            const string deleteNewRoleCctSql = @"
                                DELETE FROM REP_ROLES_CCT_NEW
                                WHERE TRIM(ROLEID) = :new_role_id";

                            using (var deleteCmd = new OracleCommand(deleteNewRoleCctSql, conn))
                            {
                                deleteCmd.Transaction = transaction;
                                deleteCmd.BindByName = true;
                                deleteCmd.Parameters.Add("new_role_id", OracleDbType.Varchar2).Value = normalizedRoleId;
                                deleteCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // If RoleId is the same, simply overwrite the cost centres for this RoleId
                            const string deleteRoleCctSql = @"
                                DELETE FROM REP_ROLES_CCT_NEW
                                WHERE TRIM(ROLEID) = :role_id";

                            using (var deleteCmd = new OracleCommand(deleteRoleCctSql, conn))
                            {
                                deleteCmd.Transaction = transaction;
                                deleteCmd.BindByName = true;
                                deleteCmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = normalizedRoleId;
                                deleteCmd.ExecuteNonQuery();
                            }
                        }

                        const string insertRoleCctSql = @"
                            INSERT INTO REP_ROLES_CCT_NEW
                            (
                                ROLEID,
                                COSTCENTRE,
                                LVL_NO,
                                STATUS
                            )
                            VALUES
                            (
                                :role_id,
                                :costcentre,
                                :lvl_no,
                                2
                            )";

                        foreach (var costCentre in costCentres)
                        {
                            int lvlNo = GetCostCentreLvlNo(conn, transaction, costCentre);

                            using (var insertCmd = new OracleCommand(insertRoleCctSql, conn))
                            {
                                insertCmd.Transaction = transaction;
                                insertCmd.BindByName = true;

                                insertCmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = normalizedRoleId;
                                insertCmd.Parameters.Add("costcentre", OracleDbType.Varchar2).Value = costCentre;
                                insertCmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = lvlNo;

                                insertCmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in UpdateRole: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public bool DeleteRole(string epfNo, string userType)
        {
            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string checkRoleSql = @"
                            SELECT COUNT(1)
                            FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :epf_no
                              AND CASE WHEN UPPER(TRIM(USERTYPE)) = 'ADMINISTRATOR' THEN 'ADMIN' ELSE UPPER(TRIM(USERTYPE)) END = :user_type";

                        using (var checkCmd = new OracleCommand(checkRoleSql, conn))
                        {
                            checkCmd.Transaction = transaction;
                            checkCmd.BindByName = true;
                            checkCmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = epfNo?.Trim();
                            checkCmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = NormalizeRoleUserType(userType);

                            var roleCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            if (roleCount == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        // Fetch the roleId of the user being deleted first
                        string roleId = null;
                        const string getRoleIdSql = @"
                            SELECT TRIM(ROLEID)
                            FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :epf_no
                              AND CASE WHEN UPPER(TRIM(USERTYPE)) = 'ADMINISTRATOR' THEN 'ADMIN' ELSE UPPER(TRIM(USERTYPE)) END = :user_type";

                        using (var getCmd = new OracleCommand(getRoleIdSql, conn))
                        {
                            getCmd.Transaction = transaction;
                            getCmd.BindByName = true;
                            getCmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = epfNo?.Trim();
                            getCmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = NormalizeRoleUserType(userType);
                            roleId = getCmd.ExecuteScalar()?.ToString()?.Trim();
                        }

                        // Delete the user role from REP_ROLE_NEW first
                        const string deleteRoleSql = @"
                            DELETE FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :epf_no
                              AND CASE WHEN UPPER(TRIM(USERTYPE)) = 'ADMINISTRATOR' THEN 'ADMIN' ELSE UPPER(TRIM(USERTYPE)) END = :user_type";

                        using (var cmd = new OracleCommand(deleteRoleSql, conn))
                        {
                            cmd.Transaction = transaction;
                            cmd.BindByName = true;
                            cmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = epfNo?.Trim();
                            cmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = NormalizeRoleUserType(userType);

                            var affectedRows = cmd.ExecuteNonQuery();
                            if (affectedRows == 0)
                            {
                                transaction.Rollback();
                                return false;
                            }
                        }

                        // Check if the roleId is still in use by any other record in REP_ROLE_NEW
                        if (!string.IsNullOrWhiteSpace(roleId))
                        {
                            const string checkRoleIdInUseSql = @"
                                SELECT COUNT(1)
                                FROM REP_ROLE_NEW
                                WHERE TRIM(ROLEID) = :role_id";

                            bool roleIdStillInUse = false;
                            using (var checkUseCmd = new OracleCommand(checkRoleIdInUseSql, conn))
                            {
                                checkUseCmd.Transaction = transaction;
                                checkUseCmd.BindByName = true;
                                checkUseCmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId;
                                roleIdStillInUse = Convert.ToInt32(checkUseCmd.ExecuteScalar()) > 0;
                            }

                            // If not in use, delete the cost centres for that roleId
                            if (!roleIdStillInUse)
                            {
                                const string deleteRoleCctSql = @"
                                    DELETE FROM REP_ROLES_CCT_NEW
                                    WHERE TRIM(ROLEID) = :role_id";

                                using (var cmd = new OracleCommand(deleteRoleCctSql, conn))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.BindByName = true;
                                    cmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in DeleteRole: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        public int AddCostCentresToRole(string epfNo, string userType, List<string> requestedCostCentres)
        {
            var normalizedCostCentres = NormalizeCostCentres(requestedCostCentres);

            if (normalizedCostCentres.Count == 0)
            {
                return 0;
            }

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string roleIdSql = @"
                            SELECT TRIM(ROLEID)
                            FROM REP_ROLE_NEW
                            WHERE TRIM(EPF_NO) = :epf_no
                              AND CASE WHEN UPPER(TRIM(USERTYPE)) = 'ADMINISTRATOR' THEN 'ADMIN' ELSE UPPER(TRIM(USERTYPE)) END = :user_type";

                        string roleId = null;

                        using (var roleCmd = new OracleCommand(roleIdSql, conn))
                        {
                            roleCmd.Transaction = transaction;
                            roleCmd.BindByName = true;
                            roleCmd.Parameters.Add("epf_no", OracleDbType.Varchar2).Value = epfNo?.Trim();
                            roleCmd.Parameters.Add("user_type", OracleDbType.Varchar2).Value = NormalizeRoleUserType(userType);

                            roleId = roleCmd.ExecuteScalar()?.ToString()?.Trim();
                        }

                        if (string.IsNullOrWhiteSpace(roleId))
                        {
                            transaction.Rollback();
                            return -1;
                        }

                        var existingCostCentres = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        const string existingSql = @"
                            SELECT TRIM(COSTCENTRE)
                            FROM REP_ROLES_CCT_NEW
                            WHERE TRIM(ROLEID) = :role_id";

                        using (var existingCmd = new OracleCommand(existingSql, conn))
                        {
                            existingCmd.Transaction = transaction;
                            existingCmd.BindByName = true;
                            existingCmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId;

                            using (var reader = existingCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var existing = reader[0]?.ToString()?.Trim();
                                    if (!string.IsNullOrWhiteSpace(existing))
                                    {
                                        existingCostCentres.Add(existing);
                                    }
                                }
                            }
                        }

                        const string insertSql = @"
                            INSERT INTO REP_ROLES_CCT_NEW
                            (
                                ROLEID,
                                COSTCENTRE,
                                LVL_NO,
                                STATUS
                            )
                            VALUES
                            (
                                :role_id,
                                :costcentre,
                                :lvl_no,
                                2
                            )";

                        int addedCount = 0;

                        foreach (var costCentre in normalizedCostCentres)
                        {
                            if (existingCostCentres.Contains(costCentre))
                            {
                                continue;
                            }

                            int lvlNo = GetCostCentreLvlNo(conn, transaction, costCentre);

                            using (var insertCmd = new OracleCommand(insertSql, conn))
                            {
                                insertCmd.Transaction = transaction;
                                insertCmd.BindByName = true;
                                insertCmd.Parameters.Add("role_id", OracleDbType.Varchar2).Value = roleId;
                                insertCmd.Parameters.Add("costcentre", OracleDbType.Varchar2).Value = costCentre;
                                insertCmd.Parameters.Add("lvl_no", OracleDbType.Int32).Value = lvlNo;
                                insertCmd.ExecuteNonQuery();
                            }

                            existingCostCentres.Add(costCentre);
                            addedCount++;
                        }

                        transaction.Commit();
                        return addedCount;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine($"Error in AddCostCentresToRole: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public List<MotherCompanyOptionModel> GetMotherCompanies()
        {
            var companies = new List<MotherCompanyOptionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                                SELECT comp_id AS CompanyId,
                                             comp_nm AS CompanyName
                                FROM glcompm
                                WHERE status = 2
                                ORDER BY comp_nm";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                companies.Add(new MotherCompanyOptionModel
                                {
                                    CompanyId = reader["CompanyId"]?.ToString(),
                                    CompanyName = reader["CompanyName"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetMotherCompanies: {ex.Message}");
                throw;
            }

            return companies;
        }

        public List<CostCentreOptionModel> GetCostCentresByCompany(string companyId)
        {
            var costCentres = new List<CostCentreOptionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            TRIM(dept_id) || ':' || TRIM(dept_nm) AS CostCentreDisplay,
                            TRIM(dept_id) AS CostCentreId,
                            TRIM(dept_nm) AS CostCentreName
                        FROM gldeptm
                        WHERE status = 2 AND (
                            :comp_id = 'ALL' OR TRIM(comp_id) IN (
                                SELECT TRIM(comp_id)
                                FROM glcompm
                                WHERE status = 2
                                  AND :comp_id IN (TRIM(parent_id), TRIM(grp_comp), TRIM(comp_id))
                            )
                        )
                        UNION ALL
                        SELECT 
                            TRIM(comp_id) || ':' || TRIM(comp_nm) AS CostCentreDisplay,
                            TRIM(comp_id) AS CostCentreId,
                            TRIM(comp_nm) AS CostCentreName
                        FROM glcompm
                        WHERE status = 2
                          AND (:comp_id = 'ALL' OR :comp_id IN (TRIM(parent_id), TRIM(grp_comp), TRIM(comp_id)))
                        ORDER BY 2";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("comp_id", OracleDbType.Varchar2).Value = companyId?.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                costCentres.Add(new CostCentreOptionModel
                                {
                                    CostCentreId = reader["CostCentreId"]?.ToString(),
                                    CostCentreName = reader["CostCentreName"]?.ToString(),
                                    CostCentreDisplay = reader["CostCentreDisplay"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCostCentresByCompany: {ex.Message}");
                throw;
            }

            return costCentres;
        }

        public List<DepartmentOptionModel> GetDepartmentsByCompany(string companyId)
        {
            var departments = new List<DepartmentOptionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT DISTINCT TRIM(dept_id) AS DepartmentId, TRIM(dept_nm) AS DepartmentName
                        FROM gldeptm
                        WHERE status = 2 AND TRIM(comp_id) = :comp_id
                        ORDER BY TRIM(dept_id)";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("comp_id", OracleDbType.Varchar2).Value = companyId?.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                departments.Add(new DepartmentOptionModel
                                {
                                    DepartmentId = reader["DepartmentId"]?.ToString(),
                                    DepartmentName = reader["DepartmentName"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetDepartmentsByCompany: {ex.Message}");
                throw;
            }

            return departments;
        }

        public List<UserGroupOptionModel> GetUserGroups()
        {
            var groups = new List<UserGroupOptionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                                SELECT USER_ROLE_ID, USER_ROLE_NAME 
                                FROM REP_USER_ROLE_NEW 
                                ORDER BY USER_ROLE_ID";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                groups.Add(new UserGroupOptionModel
                                {
                                    UserGroupId = reader["USER_ROLE_ID"]?.ToString()?.Trim(),
                                    UserGroupName = reader["USER_ROLE_NAME"]?.ToString()?.Trim()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetUserGroups: {ex.Message}");
                throw;
            }

            return groups;
        }

        public List<CostCentreOptionModel> GetCostCentresByDepartment(string departmentId)
        {
            var costCentres = new List<CostCentreOptionModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        SELECT 
                            TRIM(dept_id) || ':' || TRIM(dept_nm) AS CostCentreDisplay,
                            TRIM(dept_id) AS CostCentreId,
                            TRIM(dept_nm) AS CostCentreName
                        FROM gldeptm
                        WHERE status = 2 AND TRIM(parent_dept_id) = :dept_id
                        ORDER BY TRIM(dept_id)";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = departmentId?.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                costCentres.Add(new CostCentreOptionModel
                                {
                                    CostCentreId = reader["CostCentreId"]?.ToString(),
                                    CostCentreName = reader["CostCentreName"]?.ToString(),
                                    CostCentreDisplay = reader["CostCentreDisplay"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCostCentresByDepartment: {ex.Message}");
                throw;
            }

            return costCentres;
        }
    }
}