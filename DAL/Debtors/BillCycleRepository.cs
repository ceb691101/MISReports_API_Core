// DAL/BillCycleRepository.cs
using MISReports_Api.Models;
using System;
using MISReports_Api.Helpers;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL
{
    public class BillCycleRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixConnection"].ConnectionString;

        public BillCycleModel GetLast24BillCycles()
        {
            var model = new BillCycleModel();

            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Get max bill cycle as integer
                    string sql = "SELECT max(bill_cycle) FROM ageacct";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
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
                catch (OleDbException ex)
                {
                    // Log error appropriately for .NET 4.7.2
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

        

        public CustomerTypeModel GetCustomerTypeDescription(string custType)
        {
            var model = new CustomerTypeModel { CustType = custType };

            if (string.IsNullOrEmpty(custType))
            {
                model.Description = "Unknown";
                return model;
            }

            switch (custType.ToUpper())
            {
                case "A":
                    model.Description = "Debtors Age Analysis – Active Customers";
                    break;

                case "G":
                    model.Description = "Debtors Age Analysis – Government Customers";
                    break;

                case "F":
                    model.Description = "Debtors Age Analysis – Finalized Customers";
                    break;

                default:
                    model.Description = "Debtors Age Analysis – " + custType + " Customers";
                    break;
            }

            return model;
        }
    }
}