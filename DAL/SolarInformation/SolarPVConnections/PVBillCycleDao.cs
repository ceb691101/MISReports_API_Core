using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.SolarInformation.SolarPVConnections
{
    public class PVBillCycleDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public BillCycleModel GetLast24BillCycles()
        {
            var model = new BillCycleModel();

            try
            {
                // Test the connection first
                bool connectionTest = _dbConnection.TestConnection(out string testError, true);
                if (!connectionTest)
                {
                    model.ErrorMessage = $"Connection test failed: {testError}";
                    return model;
                }

                using (var conn = _dbConnection.GetConnection(useBulkConnection: true))
                {
                    conn.Open();
                    System.Diagnostics.Trace.WriteLine("Database connection opened successfully");

                    // Get max bill cycle as integer
                    string sql = "SELECT max(bill_cycle) FROM netmtcons";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        System.Diagnostics.Trace.WriteLine($"Query executed, result: {maxCycleObj}");

                        if (maxCycleObj != null && maxCycleObj != DBNull.Value)
                        {
                            int maxCycle;
                            if (int.TryParse(maxCycleObj.ToString(), out maxCycle))
                            {
                                model.MaxBillCycle = maxCycle.ToString();
                                model.BillCycles = BillCycleHelper.Generate24MonthYearStrings(maxCycle);
                                System.Diagnostics.Trace.WriteLine($"Successfully retrieved max bill cycle: {maxCycle}");
                            }
                            else
                            {
                                model.ErrorMessage = "Failed to parse bill cycle value";
                            }
                        }
                        else
                        {
                            model.ErrorMessage = "No bill cycle data found in netmtcons table";
                        }
                    }
                }
            }
            catch (OleDbException ex)
            {
                string errorDetails = $"OleDb Error: {ex.Message}, Error Code: {ex.ErrorCode}";
                System.Diagnostics.Trace.WriteLine(errorDetails);
                model.ErrorMessage = $"Database error: {ex.Message}";
            }
            catch (Exception ex)
            {
                string errorDetails = $"Unexpected error: {ex.Message}, Stack: {ex.StackTrace}";
                System.Diagnostics.Trace.WriteLine(errorDetails);
                model.ErrorMessage = $"Unexpected error: {ex.Message}";
            }

            return model;
        }

        
    }
}
