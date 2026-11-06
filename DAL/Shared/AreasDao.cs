using MISReports_Api.Compat;
using MISReports_Api.Models.SolarInformation;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace MISReports_Api.DAL.Shared
{
    public class AreasDao
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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

                    string pPad = null;
                    string pNoPad = null;

                    if (!string.IsNullOrWhiteSpace(regionCode))
                    {
                        sql += " WHERE region = ?";
                    }
                    else if (!string.IsNullOrWhiteSpace(provCode))
                    {
                        string pTrim = provCode.Trim();
                        if (int.TryParse(pTrim, out int pInt))
                        {
                            pPad = pInt.ToString("D2");
                            pNoPad = pInt.ToString();
                            sql += " WHERE (prov_code = ? OR prov_code = ?)";
                        }
                        else
                        {
                            pPad = pTrim;
                            sql += " WHERE prov_code = ?";
                        }
                    }

                    sql += " ORDER BY area_name";

                    using (var cmd = Informix.CreateCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(regionCode))
                        {
                            cmd.Parameters.AddWithValue("?", regionCode);
                        }
                        else if (!string.IsNullOrWhiteSpace(provCode))
                        {
                            if (pNoPad != null)
                            {
                                cmd.Parameters.AddWithValue("?", pPad);
                                cmd.Parameters.AddWithValue("?", pNoPad);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("?", pPad);
                            }
                        }

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
                catch (DbException ex)
                {
                    logger.Error(ex, "GetAreas failed (exception rethrown). regionCode={regionCode}, provCode={provCode}", regionCode, provCode);
                    throw new Exception("Error retrieving areas data: " + ex.Message, ex);
                }
            }

            return areasList;
        }
    }
}