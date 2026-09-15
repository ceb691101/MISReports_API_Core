using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class ProvinceOrdinaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage);
        }

        // regionCode is optional. When provided, only provinces that have at
        // least one area within that region are returned (used for level-based
        // access restriction). prov_servers has no region column itself, so we
        // join through areas (which carries both prov_code and region) to
        // find the matching provinces. Leaving regionCode null preserves the
        // original unrestricted behaviour used by every other report.
        public List<ProvinceModel> GetProvince(string regionCode = null)
        {
            var provinceList = new List<ProvinceModel>();

            using (var conn = _dbConnection.GetConnection(false))
            {
                try
                {
                    conn.Open();

                    string sql;
                    if (!string.IsNullOrWhiteSpace(regionCode))
                    {
                        sql = "SELECT DISTINCT ps.prov_name, ps.prov_code " +
                              "FROM prov_servers ps, areas a " +
                              "WHERE ps.prov_code = a.prov_code " +
                              "AND ps.prov_code NOT IN ('0','Z') " +
                              "AND a.region = ?";
                    }
                    else
                    {
                        sql = "Select * from prov_servers where prov_code not in('0','Z')";
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
                                    ProvinceCode = reader[1]?.ToString().Trim(),
                                    ProvinceName = reader[0]?.ToString().Trim()
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