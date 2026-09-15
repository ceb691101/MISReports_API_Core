using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class ProvinceDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage);
        }

        // regionCode is optional. When provided, only provinces that have at
        // least one area within that region are returned (used for level-based
        // access restriction). provinces has no region column itself, so we
        // join through areas (which carries both prov_code and region) to
        // find the matching provinces. Leaving regionCode null preserves the
        // original unrestricted behaviour used by every other report.
        public List<ProvinceModel> GetProvince(string regionCode = null)
        {
            var provinceList = new List<ProvinceModel>();

            using (var conn = _dbConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    string sql;
                    if (!string.IsNullOrWhiteSpace(regionCode))
                    {
                        sql = "SELECT DISTINCT p.prov_code, p.prov_name " +
                              "FROM provinces p, areas a " +
                              "WHERE p.prov_code = a.prov_code " +
                              "AND a.region = ? " +
                              "ORDER BY p.prov_name";
                    }
                    else
                    {
                        sql = "SELECT prov_code,prov_name FROM provinces ORDER BY prov_name";
                    }

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(regionCode))
                            cmd.Parameters.AddWithValue("?", regionCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var province = new ProvinceModel
                                {
                                    ProvinceCode = reader[0]?.ToString().Trim(),
                                    ProvinceName = reader[1]?.ToString().Trim()
                                };

                                provinceList.Add(province);
                            }
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    throw new Exception("Error retrieving province data: " + ex.Message, ex);
                }
            }

            return provinceList;
        }
    }
}