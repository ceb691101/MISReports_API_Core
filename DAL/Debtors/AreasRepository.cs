using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL
{
    public class AreasRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixConnection"].ConnectionString;

        // regionCode / provCode are optional. When provided, results are scoped
        // to that region or province (used for level-based access restriction).
        // Leaving both null preserves the original unrestricted behaviour used
        // by every other report calling this method.
        public List<AreaModel> GetAreas(string regionCode = null, string provCode = null)
        {
            var areasList = new List<AreaModel>();

            using (var conn = new OleDbConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    string sql = "SELECT area_code, area_name, prov_code, region FROM areas";

                    if (!string.IsNullOrWhiteSpace(regionCode))
                        sql += " WHERE region = ?";
                    else if (!string.IsNullOrWhiteSpace(provCode))
                        sql += " WHERE prov_code = ?";

                    sql += " ORDER BY area_name";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(regionCode))
                            cmd.Parameters.AddWithValue("?", regionCode);
                        else if (!string.IsNullOrWhiteSpace(provCode))
                            cmd.Parameters.AddWithValue("?", provCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var area = new AreaModel
                                {
                                    AreaCode = reader[0]?.ToString().Trim(),
                                    AreaName = reader[1]?.ToString().Trim(),
                                    ProvCode = reader[2]?.ToString().Trim(),
                                    Region = reader[3]?.ToString().Trim()
                                };

                                areasList.Add(area);
                            }
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    Console.WriteLine($"Error retrieving areas data: {ex.Message}", ex);
                    throw;
                }
            }

            return areasList;
        }
    }
}