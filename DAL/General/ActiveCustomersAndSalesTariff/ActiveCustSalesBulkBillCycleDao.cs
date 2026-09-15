using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff
{
    public class ActiveCustSalesBulkBillCycleDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        public BillCycleModel GetLast36BillCycles()
        {
            var model = new BillCycleModel();

            using (var conn = _dbConnection.GetConnection(true))
            {
                try
                {
                    conn.Open();

                    // Get max bill cycle as integer
                    string sql = "Select max(bill_cycle) from account_info";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        object maxCycleObj = cmd.ExecuteScalar();
                        if (maxCycleObj != null && maxCycleObj != DBNull.Value)
                        {
                            int maxCycle;
                            if (int.TryParse(maxCycleObj.ToString(), out maxCycle))
                            {
                                model.MaxBillCycle = maxCycle.ToString();

                                // Generate 36 months (3 years) inline without touching BillCycleHelper
                                var billCycles = new List<string>();
                                for (int i = maxCycle; i > maxCycle - 36 && i > 0; i--)
                                {
                                    billCycles.Add(BillCycleHelper.ConvertToMonthYear(i));
                                }
                                model.BillCycles = billCycles;
                            }
                        }
                    }
                }
                catch (OleDbException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Error retrieving max bill cycle: {ex.Message}");
                    model.ErrorMessage = "Error retrieving max bill cycle";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Unexpected error: {ex.Message}");
                    model.ErrorMessage = "Unexpected error occurred";
                }
            }

            return model;
        }
    }
}