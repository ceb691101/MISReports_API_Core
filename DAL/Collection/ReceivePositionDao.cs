using MISReports_Api.DBAccess;
using MISReports_Api.Models.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Collection.ReceivablePosition
{
    public class ReceivablePositionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            // receive_position is on the ordinary connection
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        /// <summary>
        /// Returns a single-row ReceivablePositionModel for one area + bill cycle + bill type.
        /// Returns null (with no throw) when no rows are found.
        /// </summary>
        public ReceivablePositionModel GetReceivablePositionReport(ReceivablePositionRequest request)
        {
            try
            {
                logger.Info("=== START GetReceivablePositionReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, AreaCode={request.AreaCode}, BillType={request.BillType}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    return GetAreaData(conn, request);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetReceivablePositionReport");
                throw;
            }
        }

        /// <summary>
        /// Returns all areas that belong to a given province (for Province report type).
        /// Routes to ordinary or bulk connection based on billType.
        /// </summary>
        public List<AreaInfoModel> GetAreasByProvince(string provinceCode, bool isBulk)
        {
            var areas = new List<AreaInfoModel>();

            try
            {
                using (var conn = _dbConnection.GetConnection(isBulk))
                {
                    conn.Open();

                    string sql = @"SELECT area_code, area_name
                                   FROM areas
                                   WHERE TRIM(prov_code) = TRIM(?)
                                   ORDER BY area_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@prov_code", provinceCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                areas.Add(new AreaInfoModel
                                {
                                    AreaCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                                    AreaName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error fetching areas for province {provinceCode}");
                throw;
            }

            return areas;
        }

        /// <summary>
        /// Returns all areas that belong to a given region.
        /// Routes to ordinary or bulk connection based on billType.
        /// </summary>
        public List<AreaInfoModel> GetAreasByRegion(string regionCode, bool isBulk)
        {
            var areas = new List<AreaInfoModel>();

            try
            {
                using (var conn = _dbConnection.GetConnection(isBulk))
                {
                    conn.Open();

                    string sql = @"SELECT area_code, area_name
                                   FROM areas
                                   WHERE TRIM(region) = TRIM(?)
                                   ORDER BY area_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@region", regionCode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                areas.Add(new AreaInfoModel
                                {
                                    AreaCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                                    AreaName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error fetching areas for region {regionCode}");
                throw;
            }

            return areas;
        }

        /// <summary>
        /// Returns all areas (for Entire CEB report type).
        /// </summary>
        public List<AreaInfoModel> GetAllAreas()
        {
            var areas = new List<AreaInfoModel>();

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string sql = "SELECT area_code, area_name FROM areas ORDER BY area_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            areas.Add(new AreaInfoModel
                            {
                                AreaCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                                AreaName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching all areas");
                throw;
            }

            return areas;
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private ReceivablePositionModel GetAreaData(OleDbConnection conn, ReceivablePositionRequest request)
        {
            string sql = @"SELECT op_bal, mon_chg, debits, credits, 
                                  un_chg, ov_chg, payments, close_bal, 
                                  cl_bal_fin, avg_3, no_months, mon_wofin
                           FROM receive_position
                           WHERE area_code = ?
                             AND bill_cycle = ?
                             AND bill_type  = ?";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@area_code", request.AreaCode);
                cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                cmd.Parameters.AddWithValue("@bill_type", request.BillType.ToUpper());

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapReaderToModel(reader, request);
                    }
                }
            }

            // No rows – return null so the caller can skip this area
            logger.Warn($"No data in receive_position for area={request.AreaCode}, cycle={request.BillCycle}, type={request.BillType}");
            return null;
        }

        private ReceivablePositionModel MapReaderToModel(OleDbDataReader reader, ReceivablePositionRequest request)
        {
            var model = new ReceivablePositionModel();

            try
            {
                model.AreaCode = request.AreaCode;
                model.BillCycle = request.BillCycle;
                model.BillType = request.BillType;

                model.RawOpeningBalance = GetDecimal(reader, 0);
                model.RawMonthlyCharge = GetDecimal(reader, 1);
                model.RawDebits = GetDecimal(reader, 2);
                model.RawCredits = GetDecimal(reader, 3);
                model.RawUnderCharge = GetDecimal(reader, 4);
                model.RawOverCharge = GetDecimal(reader, 5);
                model.RawPayments = GetDecimal(reader, 6);
                model.RawClosingBalance = GetDecimal(reader, 7);
                model.RawClosingBalanceWithoutFinAcc = GetDecimal(reader, 8);
                model.RawAverageCharge = GetDecimal(reader, 9);
                model.RawNoOfMonthsInArrears = GetDecimal(reader, 10);
                model.RawNoOfMonthsInArrearsWithoutFinAcc = GetDecimal(reader, 11);

                model.OpeningBalance = FormatDecimal(model.RawOpeningBalance);
                model.MonthlyCharge = FormatDecimal(model.RawMonthlyCharge);
                model.Debits = FormatDecimal(model.RawDebits);
                model.Credits = FormatDecimal(model.RawCredits);
                model.UnderCharge = FormatDecimal(model.RawUnderCharge);
                model.OverCharge = FormatDecimal(model.RawOverCharge);
                model.Payments = FormatDecimal(model.RawPayments);
                model.ClosingBalance = FormatDecimal(model.RawClosingBalance);
                model.ClosingBalanceWithoutFinAcc = FormatDecimal(model.RawClosingBalanceWithoutFinAcc);
                model.AverageCharge = FormatDecimal(model.RawAverageCharge);
                model.NoOfMonthsInArrears = FormatDecimal(model.RawNoOfMonthsInArrears);
                model.NoOfMonthsInArrearsWithoutFinAcc = FormatDecimal(model.RawNoOfMonthsInArrearsWithoutFinAcc);

                model.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping reader to ReceivablePositionModel");
                model.ErrorMessage = ex.Message;
            }

            return model;
        }

        private decimal GetDecimal(OleDbDataReader reader, int ordinal)
        {
            try
            {
                return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch
            {
                return 0m;
            }
        }

        private string FormatDecimal(decimal value)
        {
            try { return value.ToString("###,###.##"); }
            catch { return "0.00"; }
        }
    }

    // ── Helper model used internally ───────────────────────────────────────────
    public class AreaInfoModel
    {
        public string AreaCode { get; set; }
        public string AreaName { get; set; }
    }
}