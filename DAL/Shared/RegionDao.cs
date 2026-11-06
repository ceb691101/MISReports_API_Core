using MISReports_Api.Compat;
using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace MISReports_Api.DAL.Shared
{
    public class RegionDao
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DBConnection _dbConnection = new DBConnection();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage);
        }

        public List<RegionModel> GetRegion()
        {
            var regionList = new List<RegionModel>();

            using (var conn = _dbConnection.GetConnection())
            {
                try
                {
                    conn.Open();

                    string sql = "SELECT distinct region FROM areas WHERE region NOT IN ('L','T1')";

                    using (var cmd = Informix.CreateCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var region = new RegionModel
                            {
                                RegionCode = reader[0]?.ToString().Trim()
                            };

                            regionList.Add(region);
                        }
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "GetRegion failed (exception rethrown)");
                    throw new Exception("Error retrieving region data: " + ex.Message, ex);
                }
            }

            return regionList;
        }
    }
}
