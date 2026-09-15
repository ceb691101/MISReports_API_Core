using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.PUCSLReports.PUCSLSolarConnection
{
    /// <summary>
    /// DAO for Net Metering Report.
    /// 
    /// Groups data by tariff_category (D, GP, H, I, R, GV) and aggregates:
    /// - Ordinary customers and units (from netmtcons with tariff_code)
    /// - Bulk customers and units (from netmtcons with tariff)
    /// 
    /// Only includes Net Metering data (net_type=1).
    /// </summary>
    public class NetMeteringDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestAllConnections(out errorMessage);
        }

        // ================================================================
        //  PUBLIC ENTRY POINT
        // ================================================================
        public NetMeteringResponse GetNetMeteringReport(PUCSLRequest request)
        {
            var response = new NetMeteringResponse
            {
                Data = new List<NetMeteringData>()
            };

            try
            {
                logger.Info("=== START GetNetMeteringReport ===");
                logger.Info($"Category={request.ReportCategory}, TypeCode={request.TypeCode}, BillCycle={request.BillCycle}");

                SolarReportType reportType = MapReportType(request.ReportCategory);

                // Get Year and Month from BillCycle
                var (year, month) = GetYearMonthFromCycle(request.BillCycle);

                // Get all tariff categories in order
                var categories = GetTariffCategories();
                if (categories.Count == 0)
                {
                    logger.Warn("No tariff categories found.");
                    response.ErrorMessage = "No tariff categories found in database.";
                    return response;
                }

                // Process each category
                foreach (var category in categories)
                {
                    var categoryData = new NetMeteringData
                    {
                        Category = category,
                        Year = year,
                        Month = month,
                        NoOfCustomers = 0,
                        UnitsDayKwh = 0,
                        UnitsPeakKwh = 0,
                        UnitsOffPeakKwh = 0
                    };

                    // Get Ordinary tariff codes for this category
                    var ordinaryTariffs = GetTariffCodesForCategory(category, "O");
                    foreach (var tariffCode in ordinaryTariffs)
                    {
                        var ordData = GetOrdinaryData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                        categoryData.NoOfCustomers += ordData.customers;
                        categoryData.UnitsDayKwh += ordData.unitsDay;
                    }

                    // Get Bulk tariff codes for this category
                    var bulkTariffs = GetTariffCodesForCategory(category, "B");
                    foreach (var tariffCode in bulkTariffs)
                    {
                        var bulkData = GetBulkData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                        categoryData.NoOfCustomers += bulkData.customers;
                        categoryData.UnitsDayKwh += bulkData.unitsDay;
                        categoryData.UnitsPeakKwh += bulkData.unitsPeak;
                        categoryData.UnitsOffPeakKwh += bulkData.unitsOffPeak;
                    }

                    response.Data.Add(categoryData);
                }

                // Calculate Total
                var total = new NetMeteringData
                {
                    Category = "Total",
                    Year = year,
                    Month = month,
                    NoOfCustomers = 0,
                    UnitsDayKwh = 0,
                    UnitsPeakKwh = 0,
                    UnitsOffPeakKwh = 0
                };

                foreach (var row in response.Data)
                {
                    total.NoOfCustomers += row.NoOfCustomers;
                    total.UnitsDayKwh += row.UnitsDayKwh;
                    total.UnitsPeakKwh += row.UnitsPeakKwh;
                    total.UnitsOffPeakKwh += row.UnitsOffPeakKwh;
                }

                response.Total = total;

                logger.Info($"Net Metering Report completed. {response.Data.Count} categories.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetNetMeteringReport EXCEPTION");
                response.ErrorMessage = $"Error: {ex.Message}";
            }

            return response;
        }

        // ================================================================
        //  GET TARIFF CATEGORIES
        // ================================================================

        /// <summary>
        /// Gets all tariff categories ordered by sequence.
        /// Example: D, GP, H, I, R, GV
        /// </summary>
        private List<string> GetTariffCategories()
        {
            var categories = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string sql = "SELECT tariff_cat FROM tariff_category ORDER BY seq";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var category = reader[0]?.ToString().Trim();
                            if (!string.IsNullOrEmpty(category))
                            {
                                categories.Add(category);
                            }
                        }
                    }
                }
                logger.Info($"Found {categories.Count} tariff categories");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetTariffCategories EXCEPTION");
            }
            return categories;
        }

        // ================================================================
        //  GET TARIFF CODES FOR CATEGORY
        // ================================================================

        /// <summary>
        /// Gets all tariff codes for a specific category and customer type.
        /// </summary>
        /// <param name="category">Tariff category (e.g., "D", "GP")</param>
        /// <param name="cusCategory">"O" for Ordinary, "B" for Bulk</param>
        private List<string> GetTariffCodesForCategory(string category, string cusCategory)
        {
            var tariffCodes = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string sql = "SELECT c.tariff_code FROM cat_tariff_table c, tariff_category t " +
                                 "WHERE c.tariff_cat=t.tariff_cat AND t.tariff_cat=? AND cus_cat=?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", category);
                        cmd.Parameters.AddWithValue("?", cusCategory);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var tariffCode = reader[0]?.ToString().Trim();
                                if (!string.IsNullOrEmpty(tariffCode))
                                {
                                    tariffCodes.Add(tariffCode);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetTariffCodesForCategory cat={category}, cusCat={cusCategory}");
            }
            return tariffCodes;
        }

        // ================================================================
        //  GET ORDINARY DATA
        // ================================================================

        /// <summary>
        /// Gets ordinary customer count and units for a specific tariff code.
        /// Uses calc_cycle and tariff_code fields.
        /// </summary>
        private (int customers, decimal unitsDay) GetOrdinaryData(SolarReportType rt, string typeCode, 
            string calcCycle, string tariffCode)
        {
            int customers = 0;
            decimal unitsDay = 0;

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = "SELECT COUNT(a.acct_number), COALESCE(SUM(a.units_out),0) " +
                                  "FROM netmtcons a, areas r " +
                                  "WHERE a.calc_cycle=? AND a.net_type=1 AND a.tariff_code=? " +
                                  "AND r.area_code=a.area_code AND r.prov_code=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        case SolarReportType.Region:
                            sql = "SELECT COUNT(a.acct_number), COALESCE(SUM(a.units_out),0) " +
                                  "FROM netmtcons a, areas r " +
                                  "WHERE a.calc_cycle=? AND a.net_type=1 AND a.tariff_code=? " +
                                  "AND r.area_code=a.area_code AND r.region=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        default: // EntireCEB
                            sql = "SELECT COUNT(a.acct_number), COALESCE(SUM(a.units_out),0) " +
                                  "FROM netmtcons a " +
                                  "WHERE a.calc_cycle=? AND a.net_type=1 AND a.tariff_code=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            unitsDay = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdinaryData tariffCode={tariffCode}");
            }

            return (customers, unitsDay);
        }

        // ================================================================
        //  GET BULK DATA
        // ================================================================

        /// <summary>
        /// Gets bulk customer count and units for a specific tariff.
        /// Uses bill_cycle and tariff fields.
        /// Returns Day, Peak, and Off-Peak units.
        /// </summary>
        private (int customers, decimal unitsDay, decimal unitsPeak, decimal unitsOffPeak) GetBulkData(
            SolarReportType rt, string typeCode, string billCycle, string tariff)
        {
            int customers = 0;
            decimal unitsDay = 0;
            decimal unitsPeak = 0;
            decimal unitsOffPeak = 0;

            try
            {
                string bulkTypeCode = (rt == SolarReportType.Province)
                    ? typeCode.PadLeft(2, '0')  // "3" → "03"
                    : typeCode;

                using (var conn = _dbConnection.GetConnection(true)) // Bulk connection
                {
                    conn.Open();
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = "SELECT COUNT(acc_nbr), COALESCE(SUM(exp_kwd_units),0), " +
                                  "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0) " +
                                  "FROM netmtcons n, areas a " +
                                  "WHERE bill_cycle=? AND net_type=1 " +
                                  "AND a.area_code=n.area_cd AND a.prov_code=? AND tariff=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                            break;

                        case SolarReportType.Region:
                            sql = "SELECT COUNT(acc_nbr), COALESCE(SUM(exp_kwd_units),0), " +
                                  "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0) " +
                                  "FROM netmtcons n, areas a " +
                                  "WHERE bill_cycle=? AND net_type=1 " +
                                  "AND a.area_code=n.area_cd AND a.region=? AND tariff=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                            break;

                        default: // EntireCEB
                            sql = "SELECT COUNT(acc_nbr), COALESCE(SUM(exp_kwd_units),0), " +
                                  "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0) " +
                                  "FROM netmtcons " +
                                  "WHERE bill_cycle=? AND net_type=1 AND tariff=?";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            unitsDay = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                            unitsPeak = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                            unitsOffPeak = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkData tariff={tariff}");
            }

            return (customers, unitsDay, unitsPeak, unitsOffPeak);
        }

        // ================================================================
        //  UTILITY
        // ================================================================

        private SolarReportType MapReportType(PUCSLReportCategory cat)
        {
            switch (cat)
            {
                case PUCSLReportCategory.Province: return SolarReportType.Province;
                case PUCSLReportCategory.Region: return SolarReportType.Region;
                default: return SolarReportType.EntireCEB;
            }
        }

        /// <summary>
        /// Extracts Year and Month from bill cycle using BillCycleHelper.
        /// Example: "445" -> "Sep 25" -> Year="25", Month="9"
        /// </summary>
        private (string year, string month) GetYearMonthFromCycle(string billCycle)
        {
            try
            {
                int cycle = int.Parse(billCycle);
                string monthYear = BillCycleHelper.ConvertToMonthYear(cycle); // Returns "Sep 25"

                if (string.IsNullOrEmpty(monthYear) || monthYear == "Invalid" || monthYear == "Unknown")
                {
                    logger.Warn($"Invalid bill cycle: {billCycle}");
                    return ("", "");
                }

                // Parse "Sep 25" into month number and year
                var parts = monthYear.Split(' ');
                if (parts.Length == 2)
                {
                    string monthName = parts[0];
                    string year = parts[1];

                    // Convert month name to month number
                    string monthNumber = ConvertMonthNameToNumber(monthName);

                    return (year, monthNumber); // year="25", month="9"
                }

                return ("", "");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error parsing bill cycle {billCycle}");
                return ("", "");
            }
        }

        /// <summary>
        /// Converts month name (e.g., "Sep", "Jan") to month number (e.g., "9", "1")
        /// </summary>
        private string ConvertMonthNameToNumber(string monthName)
        {
            switch (monthName)
            {
                case "Jan": return "1";
                case "Feb": return "2";
                case "Mar": return "3";
                case "Apr": return "4";
                case "May": return "5";
                case "Jun": return "6";
                case "Jul": return "7";
                case "Aug": return "8";
                case "Sep": return "9";
                case "Oct": return "10";
                case "Nov": return "11";
                case "Dec": return "12";
                default: return monthName; // Return as-is if not recognized
            }
        }
    }

    // ================================================================
    //  INTERNAL HELPER ENUM
    // ================================================================

    internal enum SolarReportType
    {
        Province,
        Region,
        EntireCEB
    }
}