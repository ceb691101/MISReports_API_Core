using MISReports_Api.Models.Shared;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using System;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Shared
{
    public class ReceivablePositionBillCycleDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();

        // ── Primary method used by CollectionController ────────────────────
        /// <summary>
        /// Returns the last 24 bill cycles from receive_position.
        /// Optionally filtered by bill_type ("O" or "B").
        /// Uses ordinary connection.
        /// </summary>
        public BillCycleModel GetLast24BillCycles(string billType = null)
        {
            var model = new BillCycleModel();

            using (var conn = _dbConnection.GetConnection(false))
            {
                try
                {
                    conn.Open();

                    string sql = string.IsNullOrWhiteSpace(billType)
                        ? "SELECT MAX(bill_cycle) FROM receive_position"
                        : "SELECT MAX(bill_cycle) FROM receive_position WHERE bill_type = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(billType))
                            cmd.Parameters.AddWithValue("@bill_type", billType.Trim().ToUpper());

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
                    System.Diagnostics.Trace.WriteLine(
                        $"Error retrieving max bill cycle from receive_position: {ex.Message}");
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

        // ── Helper: identifies bulk bill types ────────────────────────────
        public bool IsBulk(string billType)
        {
            return string.Equals(billType?.Trim(), "B", StringComparison.OrdinalIgnoreCase);
        }

        // ── Get max bill cycle string for a given bill type ───────────────
        public string GetMaxBillCycle(string billType)
        {
            bool useBulk = IsBulk(billType);
            return QueryMaxBillCycle(useBulk, billType);
        }

        // ── Return the first non-null max from multiple candidate cycles ──
        public int? FirstAvailableMax(params int?[] candidates)
        {
            foreach (var c in candidates)
                if (c.HasValue) return c;
            return null;
        }

        // ── Query max bill_cycle from receive_position ────────────────────
        public string QueryMaxBillCycle(bool useBulkConnection, string billType)
        {
            using (var conn = _dbConnection.GetConnection(true)) // always ordinary
            {
                try
                {
                    conn.Open();

                    string sql = string.IsNullOrWhiteSpace(billType)
                        ? "SELECT MAX(bill_cycle) FROM receive_position"
                        : "SELECT MAX(bill_cycle) FROM receive_position WHERE bill_type = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (!string.IsNullOrWhiteSpace(billType))
                            cmd.Parameters.AddWithValue("@bill_type", billType.Trim().ToUpper());

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return result.ToString();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"Error in QueryMaxBillCycle: {ex.Message}");
                }
            }

            return null;
        }

        // ── Query max bill_cycle from mon_tot (bulk connection) ───────────
        public string QueryMaxBillCycleFromMonTot(bool useBulkConnection)
        {
            using (var conn = _dbConnection.GetConnection(useBulkConnection))
            {
                try
                {
                    conn.Open();

                    string sql = "SELECT MAX(bill_cycle) FROM mon_tot";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return result.ToString();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"Error in QueryMaxBillCycleFromMonTot: {ex.Message}");
                }
            }

            return null;
        }
    }
}