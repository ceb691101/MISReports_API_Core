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
    public class BillCycleFromAreaDao
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DBConnection _dbConnection = new DBConnection();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        //public BillCycleModel GetLast24BillCycles()
        //{
        //    var model = new BillCycleModel();

        //    using (var conn = _dbConnection.GetConnection(false))
        //    {
        //        try
        //        {
        //            conn.Open();

        //            // Use the latest bill cycle recorded in areas.
        //            const string maxCycleSql = "SELECT MAX(bill_cycle) FROM areas";
        //            int maxCycle;

        //            using (DbCommand cmd = new DbCommand(maxCycleSql, conn))
        //            {
        //                object maxCycleObj = cmd.ExecuteScalar();
        //                if (maxCycleObj == null || maxCycleObj == DBNull.Value)
        //                {
        //                    model.ErrorMessage = "No bill cycles found in areas table";
        //                    return model;
        //                }

        //                if (!int.TryParse(maxCycleObj.ToString(), out maxCycle))
        //                {
        //                    model.ErrorMessage = "Invalid bill cycle format";
        //                    return model;
        //                }
        //            }

        //            model.MaxBillCycle = maxCycle.ToString();
        //            model.BillCycles = BillCycleHelper.Generate24MonthYearStrings(maxCycle);
        //        }

        //        catch (DbException ex)
        //        {
        //            logger.Error(ex, "GetLast24BillCycles failed");
        //            System.Diagnostics.Trace.WriteLine($"Error retrieving bill cycle: {ex.Message}");
        //            model.ErrorMessage = "Error retrieving bill cycle";
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.Error(ex, "GetLast24BillCycles failed");
        //            System.Diagnostics.Trace.WriteLine($"Unexpected error: {ex.Message}");
        //            model.ErrorMessage = "Unexpected error occurred";
        //        }
        //    }

        //    return model;
        //}
        public BillCycleModel GetLast24BillCycles()
        {
            var model = new BillCycleModel();

            using (var conn = _dbConnection.GetConnection(false))
            {
                try
                {
                    conn.Open();

                    // Get min bill cycle from areas table
                    //    string minCycleSql = "SELECT MIN(bill_cycle) FROM areas";
                    string minCycleSql = "SELECT Max(calc_cycle) FROM areas";
                    int minCycle;

                    using (DbCommand cmd = Informix.CreateCommand(minCycleSql, conn))
                    {
                        object minCycleObj = cmd.ExecuteScalar();
                        if (minCycleObj == null || minCycleObj == DBNull.Value)
                        {
                            model.ErrorMessage = "No bill cycles found in areas table";
                            return model;
                        }

                        if (!int.TryParse(minCycleObj.ToString(), out minCycle))
                        {
                            model.ErrorMessage = "Invalid bill cycle format";
                            return model;
                        }
                    }

                    // Check count of records with min bill cycle
                    string countSql = "SELECT COUNT(*) FROM areas WHERE bill_cycle = ?";
                    int count;

                    using (DbCommand cmd = Informix.CreateCommand(countSql, conn))
                    {
                        cmd.Parameters.Add("@bill_cycle", OleDbType.Integer).Value = minCycle;
                        object countObj = cmd.ExecuteScalar();
                        count = Convert.ToInt32(countObj);
                    }

                    // Determine maxCycle based on count
                    int maxCycle;
                    //if (count == 0)
                    //{
                    //    maxCycle = minCycle;
                    //}
                    //else
                    //{
                    //    maxCycle = minCycle - 1;
                    //}


                    if (count == 70)
                    {
                        maxCycle = minCycle;
                    }
                    else
                    {
                        maxCycle = minCycle - 1;
                    }
                    model.MaxBillCycle = maxCycle.ToString();
                    model.BillCycles = BillCycleHelper.Generate24MonthYearStrings(maxCycle);
                }

                catch (DbException ex)
                {
                    System.Diagnostics.Trace.WriteLine($"Error retrieving bill cycle: {ex.Message}");
                    model.ErrorMessage = "Error retrieving bill cycle";
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
