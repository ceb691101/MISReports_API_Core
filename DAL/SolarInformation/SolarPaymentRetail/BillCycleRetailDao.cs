using MISReports_Api.Compat;
using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MISReports_Api.DAL.SolarInformation.SolarPaymentRetail
{
    public class BillCycleRetailDao
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DBConnection _dbConnection = new DBConnection();

        public BillCycleModel GetLast24BillCycles()
        {
            var model = new BillCycleModel();

            using (var conn = _dbConnection.GetConnection(false))
            {
                try
                {
                    conn.Open();

                    // Get max bill cycle as integer
                    string sql = "SELECT max(bill_cycle) FROM netmtcons ";
                    using (DbCommand cmd = Informix.CreateCommand(sql, conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj != null && maxCycleObj != DBNull.Value)
                        {
                            int maxCycle;
                            if (int.TryParse(maxCycleObj.ToString(), out maxCycle))
                            {
                                model.MaxBillCycle = maxCycle.ToString();
                                model.BillCycles = BillCycleHelper.Generate24MonthYearStrings(maxCycle);
                            }
                        }
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "GetLast24BillCycles failed");
                    System.Diagnostics.Trace.WriteLine($"Error retrieving max bill cycle: {ex.Message}");
                    model.ErrorMessage = "Error retrieving max bill cycle";
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "GetLast24BillCycles failed");
                    System.Diagnostics.Trace.WriteLine($"Unexpected error: {ex.Message}");
                    model.ErrorMessage = "Unexpected error occurred";
                }
            }

            return model;
        }

        
    }
}
