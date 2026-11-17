using MISReports_Api.Compat;
using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MISReports_Api.DAL.Shared
{
    public class CalcCycleFromAreaDao
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DBConnection _dbConnection = new DBConnection();

        public CalcCycleModel GetMaxCalcCycle()
        {
            var model = new CalcCycleModel();

            using (var conn = _dbConnection.GetConnection(false))
            {
                try
                {
                    conn.Open();

                    // Get max calc cycle as integer
                    string sql = "select max(calc_cycle) from areas";
                    using (DbCommand cmd = Informix.CreateCommand(sql, conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj != null && maxCycleObj != DBNull.Value)
                        {
                            int maxCycle;
                            if (int.TryParse(maxCycleObj.ToString(), out maxCycle))
                            {
                                model.MaxCalcCycle = maxCycle.ToString();
                            }
                        }
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "GetMaxCalcCycle failed");
                    System.Diagnostics.Trace.WriteLine($"Error retrieving max calc cycle: {ex.Message}");
                    model.ErrorMessage = "Error retrieving max calc cycle";
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "GetMaxCalcCycle failed");
                    System.Diagnostics.Trace.WriteLine($"Unexpected error: {ex.Message}");
                    model.ErrorMessage = "Unexpected error occurred";
                }
            }

            return model;
        }

    }
}
