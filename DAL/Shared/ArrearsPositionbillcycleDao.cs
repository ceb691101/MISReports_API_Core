using MISReports_Api.Compat;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.General;
using System;
using System.Data.Common;

namespace MISReports_Api.DAL.Shared
{
    /// <summary>
    /// Fetches the maximum bill cycle from the <c>areas</c> table in the
    /// billsmry database (bulk connection) for a given area code, then
    /// generates the preceding 24 month/year strings via BillCycleHelper.
    /// 
    /// DB     : billsmry
    /// Table  : areas
    /// SQL    : SELECT MAX(bill_cycle) FROM areas WHERE area_code = ?
    /// </summary>
    public class ArrearsPositionBillCycleDao
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DBConnection _dbConnection = new DBConnection();

        public ArrearsPositionBillCycleModel GetMaxBillCycle(string areaCode)
        {
            var model = new ArrearsPositionBillCycleModel();

            using (var conn = _dbConnection.GetConnection(false)) // bulk connection
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                try
                {
                    string sql = "SELECT MAX(bill_cycle) FROM areas WHERE area_code = ?";

                    using (var cmd = Informix.CreateCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@area_code", areaCode);

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            int maxCycle;
                            if (int.TryParse(result.ToString(), out maxCycle))
                            {
                                model.MaxBillCycle = maxCycle.ToString();
                                model.BillCycles = BillCycleHelper.Generate24MonthYearStrings(maxCycle);
                            }
                            else
                            {
                                model.ErrorMessage = "Max bill cycle value could not be parsed.";
                            }
                        }
                        else
                        {
                            model.ErrorMessage = "No bill cycle found for the given area code.";
                        }
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "GetMaxBillCycle failed. areaCode={areaCode}", areaCode);
                    System.Diagnostics.Trace.WriteLine($"DbException in ArrearsPositionBillCycleDao: {ex.Message}");
                    model.ErrorMessage = "Database error retrieving max bill cycle.";
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "GetMaxBillCycle failed. areaCode={areaCode}", areaCode);
                    System.Diagnostics.Trace.WriteLine($"Unexpected error in ArrearsPositionBillCycleDao: {ex.Message}");
                    model.ErrorMessage = "Unexpected error occurred.";
                }
            }

            return model;
        }
    }
}