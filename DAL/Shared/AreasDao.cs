using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class AreasDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage);
        }

        // regionCode / provCode are optional. When provided, results are scoped
        // to that region or province (used for level-based access restriction).
        // Leaving both null preserves the original unrestricted behaviour used
        // by every other report calling this method.
        public List<AreaBulkModel> GetAreas(string regionCode = null, string provCode = null)
        {
            var areasList = new List<AreaBulkModel>();

            using (var conn = _dbConnection.GetConnection())
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
                                var area = new AreaBulkModel
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
                    throw new Exception("Error retrieving areas data: " + ex.Message, ex);
                }
            }

            return areasList;
        }
    }
}