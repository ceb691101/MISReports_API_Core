using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;

namespace MISReports_Api.DAL
{
    public class ReportCategoryRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private static string NormalizeCategoryCode(string catCode)
        {
            return string.IsNullOrWhiteSpace(catCode)
                ? null
                : catCode.Trim().ToUpperInvariant();
        }

        private static string NormalizeCategoryName(string catName)
        {
            return string.IsNullOrWhiteSpace(catName)
                ? null
                : catName.Trim();
        }

        /// <summary>
        /// Get all report categories
        /// </summary>
        /// <returns>List of ReportCategoryModel</returns>
        public List<ReportCategoryModel> GetAllCategories()
        {
            var categories = new List<ReportCategoryModel>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT catcode, catname
                        FROM rep_cats_new
                        ORDER BY catcode";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                categories.Add(new ReportCategoryModel
                                {
                                    CatCode = reader["CATCODE"]?.ToString(),
                                    CatName = reader["CATNAME"]?.ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetAllCategories: {ex.Message}");
                throw;
            }

            return categories;
        }

        /// <summary>
        /// Get a specific report category by code
        /// </summary>
        /// <param name="catCode">The category code</param>
        /// <returns>ReportCategoryModel if found, null otherwise</returns>
        public ReportCategoryModel GetCategoryByCode(string catCode)
        {
            ReportCategoryModel category = null;

            try
            {
                var normalizedCatCode = NormalizeCategoryCode(catCode);

                if (string.IsNullOrWhiteSpace(normalizedCatCode))
                {
                    return null;
                }

                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT catcode, catname
                        FROM rep_cats_new
                        WHERE UPPER(TRIM(catcode)) = :catCode";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("catCode", OracleDbType.Varchar2).Value = normalizedCatCode;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                category = new ReportCategoryModel
                                {
                                    CatCode = reader["CATCODE"]?.ToString(),
                                    CatName = reader["CATNAME"]?.ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCategoryByCode: {ex.Message}");
                throw;
            }

            return category;
        }

        /// <summary>
        /// Create or update a report category using MERGE statement
        /// </summary>
        /// <param name="request">CreateReportCategoryRequest object</param>
        /// <returns>true if successful, false otherwise</returns>
        public bool AddOrUpdateCategory(CreateReportCategoryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CatCode) || string.IsNullOrWhiteSpace(request.CatName))
            {
                return false;
            }

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    const string sql = @"
                        MERGE INTO rep_cats_new t
                        USING dual
                        ON (UPPER(TRIM(t.catcode)) = :catCode)
                        WHEN MATCHED THEN
                            UPDATE SET t.catname = :catDesc
                        WHEN NOT MATCHED THEN
                            INSERT (catcode, catname)
                            VALUES (:catCode, :catDesc)";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        var normalizedCatCode = NormalizeCategoryCode(request.CatCode);
                        var normalizedCatName = NormalizeCategoryName(request.CatName);

                        cmd.BindByName = true;
                        cmd.Parameters.Add("catCode", OracleDbType.Varchar2).Value = normalizedCatCode;
                        cmd.Parameters.Add("catDesc", OracleDbType.Varchar2).Value = normalizedCatName;

                        var result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in AddOrUpdateCategory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Update an existing report category
        /// </summary>
        /// <param name="request">CreateReportCategoryRequest object</param>
        /// <returns>true if successful, false otherwise</returns>
        public bool UpdateCategory(CreateReportCategoryRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.CatCode) || string.IsNullOrWhiteSpace(request.CatName))
            {
                return false;
            }

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    const string sql = @"
                        UPDATE rep_cats_new
                        SET catname = :catDesc
                        WHERE UPPER(TRIM(catcode)) = :catCode";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        var normalizedCatCode = NormalizeCategoryCode(request.CatCode);
                        var normalizedCatName = NormalizeCategoryName(request.CatName);

                        cmd.BindByName = true;
                        cmd.Parameters.Add("catCode", OracleDbType.Varchar2).Value = normalizedCatCode;
                        cmd.Parameters.Add("catDesc", OracleDbType.Varchar2).Value = normalizedCatName;

                        var result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in UpdateCategory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete a report category by code
        /// </summary>
        /// <param name="catCode">The category code to delete</param>
        /// <returns>true if successful, false otherwise</returns>
        public bool DeleteCategory(string catCode)
        {
            if (string.IsNullOrWhiteSpace(catCode))
            {
                return false;
            }

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    conn.Open();

                    const string sql = @"
                        DELETE FROM rep_cats_new
                        WHERE UPPER(TRIM(catcode)) = :catCode";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;
                        cmd.Parameters.Add("catCode", OracleDbType.Varchar2).Value = NormalizeCategoryCode(catCode);

                        var result = cmd.ExecuteNonQuery();
                        return result > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DeleteCategory: {ex.Message}");
                throw;
            }
        }
    }
}
