using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.PUCSLReports.PUCSLSolarConnection
{
    /// <summary>
    /// DAO for PUCSL Variable Solar Data Submission Report (Report 2 from PDF).
    /// 
    /// Tracks solar installations by generation capacity ranges:
    ///   - 0 < gen_cap <= 20 kW
    ///   - 20 < gen_cap <= 100 kW
    ///   - 100 < gen_cap <= 500 kW
    ///   - gen_cap > 500 kW
    ///
    /// Ordinary data -> InformixConnection     (GetConnection(false))
    /// Bulk data     -> InformixBulkConnection (GetConnection(true))
    ///
    /// IMPORTANT: Variable solar uses schm='3' (different from Fixed Solar)
    /// </summary>
    public class VariableSolarDataDao
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
        public List<VariableSolarDataModel> GetVariableSolarDataReport(PUCSLRequest request)
        {
            var results = new List<VariableSolarDataModel>();

            try
            {
                logger.Info("=== START GetVariableSolarDataReport ===");
                logger.Info($"Category={request.ReportCategory}, TypeCode={request.TypeCode}, " +
                            $"BillCycle={request.BillCycle}, SolarType={request.SolarType}");

                // Throw exception for invalid solar type
                if (request.SolarType == SolarNetType.NetMetering)
                {
                    logger.Warn("NetMetering is not supported for Variable Solar Data report");
                    throw new ArgumentException("NetMetering is not supported for Variable Solar Data report. Please use NetAccounting, NetPlus, or NetPlusPlus.");
                }

                SolarReportType reportType = MapReportType(request.ReportCategory);

                // Get net type conditions for ordinary and bulk
                string ordNetCond = GetOrdNetTypeCondition(request.SolarType);
                string bulkNetType = GetBulkNetType(request.SolarType);

                var categories = GetTariffCategories();
                if (categories.Count == 0) { logger.Warn("No tariff categories."); return results; }

                // Parse bill cycle once
                int.TryParse(request.BillCycle, out int bcInt);
                string monthYear = BillCycleHelper.ConvertToMonthYear(bcInt);
                string[] bcParts = monthYear.Split(' ');
                string rowMonth = bcParts.Length > 0 ? MonthNameToNumber(bcParts[0]) : monthYear;
                string rowYear = bcParts.Length > 1 ? bcParts[1] : "";

                foreach (var tc in categories)
                {
                    var model = new VariableSolarDataModel
                    {
                        Category = tc.TariffCatDisplay,
                        Year = rowYear,
                        Month = rowMonth
                    };

                    // ── ORDINARY ─────────────────────────────────────────────
                    var ordCodes = GetOrdTariffCodes(tc.TariffCat);
                    if (ordCodes != null && ordCodes.Count > 0)
                    {
                        // Query each capacity range
                        var ord0To20 = GetOrdCapacityRange(reportType, request.TypeCode,
                            request.BillCycle, ordCodes, ordNetCond, 0, 20);
                        var ord20To100 = GetOrdCapacityRange(reportType, request.TypeCode,
                            request.BillCycle, ordCodes, ordNetCond, 20, 100);
                        var ord100To500 = GetOrdCapacityRange(reportType, request.TypeCode,
                            request.BillCycle, ordCodes, ordNetCond, 100, 500);
                        var ordAbove500 = GetOrdCapacityRange(reportType, request.TypeCode,
                            request.BillCycle, ordCodes, ordNetCond, 500, 0);

                        model.NoOfCustomers0To20 = ord0To20.NoOfCustomers;
                        model.KwhUnits0To20 = ord0To20.KwhUnits;
                        model.PaidAmount0To20 = ord0To20.PaidAmount;

                        model.NoOfCustomers20To100 = ord20To100.NoOfCustomers;
                        model.KwhUnits20To100 = ord20To100.KwhUnits;
                        model.PaidAmount20To100 = ord20To100.PaidAmount;

                        model.NoOfCustomers100To500 = ord100To500.NoOfCustomers;
                        model.KwhUnits100To500 = ord100To500.KwhUnits;
                        model.PaidAmount100To500 = ord100To500.PaidAmount;

                        model.NoOfCustomersAbove500 = ordAbove500.NoOfCustomers;
                        model.KwhUnitsAbove500 = ordAbove500.KwhUnits;
                        model.PaidAmountAbove500 = ordAbove500.PaidAmount;
                    }

                    // ── BULK ──────────────────────────────────────────────────
                    var bulkCodes = GetBulkTariffCodes(tc.TariffCat);
                    if (bulkCodes != null && bulkCodes.Count > 0)
                    {
                        // Bulk database uses padded province codes
                        string bulkTypeCode = request.TypeCode;
                        if (reportType == SolarReportType.Province && request.TypeCode.Length == 1)
                        {
                            bulkTypeCode = request.TypeCode.PadLeft(2, '0');
                        }

                        // Query each capacity range for bulk
                        var bulk0To20 = GetBulkCapacityRange(reportType, bulkTypeCode,
                            request.BillCycle, bulkCodes, bulkNetType, 0, 20);
                        var bulk20To100 = GetBulkCapacityRange(reportType, bulkTypeCode,
                            request.BillCycle, bulkCodes, bulkNetType, 20, 100);
                        var bulk100To500 = GetBulkCapacityRange(reportType, bulkTypeCode,
                            request.BillCycle, bulkCodes, bulkNetType, 100, 500);
                        var bulkAbove500 = GetBulkCapacityRange(reportType, bulkTypeCode,
                            request.BillCycle, bulkCodes, bulkNetType, 500, 0);

                        // Add bulk to ordinary totals
                        model.NoOfCustomers0To20 += bulk0To20.NoOfCustomers;
                        model.KwhUnits0To20 += bulk0To20.KwhUnits;
                        model.PaidAmount0To20 += bulk0To20.PaidAmount;

                        model.NoOfCustomers20To100 += bulk20To100.NoOfCustomers;
                        model.KwhUnits20To100 += bulk20To100.KwhUnits;
                        model.PaidAmount20To100 += bulk20To100.PaidAmount;

                        model.NoOfCustomers100To500 += bulk100To500.NoOfCustomers;
                        model.KwhUnits100To500 += bulk100To500.KwhUnits;
                        model.PaidAmount100To500 += bulk100To500.PaidAmount;

                        model.NoOfCustomersAbove500 += bulkAbove500.NoOfCustomers;
                        model.KwhUnitsAbove500 += bulkAbove500.KwhUnits;
                        model.PaidAmountAbove500 += bulkAbove500.PaidAmount;
                    }

                    model.ErrorMessage = string.Empty;
                    results.Add(model);
                }

                logger.Info($"=== END GetVariableSolarDataReport — {results.Count} rows ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetVariableSolarDataReport");
                throw;
            }
        }

        // ================================================================
        //  NET-TYPE CONDITION HELPERS
        // ================================================================

        private string GetOrdNetTypeCondition(SolarNetType st)
        {
            switch (st)
            {
                case SolarNetType.NetAccounting: return "(n.net_type='2' OR n.net_type='5')";
                case SolarNetType.NetPlus: return "n.net_type='3'";
                case SolarNetType.NetPlusPlus: return "n.net_type='4'";
                default: return "(n.net_type='2' OR n.net_type='5')";
            }
        }

        private string GetBulkNetType(SolarNetType st)
        {
            switch (st)
            {
                case SolarNetType.NetAccounting: return "2";
                case SolarNetType.NetPlus: return "3";
                case SolarNetType.NetPlusPlus: return "4";
                default: return "2";
            }
        }

        // ================================================================
        //  TARIFF CODES
        // ================================================================

        private List<TariffCategoryItem> GetTariffCategories()
        {
            var list = new List<TariffCategoryItem>();
            using (var conn = _dbConnection.GetConnection(false))
            {
                conn.Open();
                using (var cmd = new OleDbCommand("SELECT tariff_cat FROM tariff_category ORDER BY seq", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        string cat = r[0]?.ToString().Trim();
                        list.Add(new TariffCategoryItem { TariffCat = cat, TariffCatDisplay = cat });
                    }
            }
            return list;
        }

        private List<string> GetOrdTariffCodes(string tariffCat)
        {
            var codes = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    const string sql =
                        "SELECT c.tariff_code FROM cat_tariff_table c, tariff_category t " +
                        "WHERE c.tariff_cat = t.tariff_cat AND t.tariff_cat = ? AND cus_cat = 'O'";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", tariffCat);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var code = reader[0]?.ToString().Trim();
                                if (!string.IsNullOrEmpty(code))
                                {
                                    codes.Add(code);
                                }
                            }
                        }
                    }
                }
                logger.Info($"GetOrdTariffCodes for {tariffCat}: {string.Join(", ", codes)}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdTariffCodes EXCEPTION for cat={tariffCat}");
            }
            return codes;
        }

        private List<string> GetBulkTariffCodes(string tariffCat)
        {
            var codes = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    const string sql =
                        "SELECT c.tariff_code FROM cat_tariff_table c, tariff_category t " +
                        "WHERE c.tariff_cat = t.tariff_cat AND t.tariff_cat = ? AND cus_cat = 'B'";
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", tariffCat);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var code = reader[0]?.ToString().Trim();
                                if (!string.IsNullOrEmpty(code))
                                {
                                    codes.Add(code);
                                }
                            }
                        }
                    }
                }
                logger.Info($"GetBulkTariffCodes for {tariffCat}: {string.Join(", ", codes)}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkTariffCodes EXCEPTION for cat={tariffCat}");
            }
            return codes;
        }

        // ================================================================
        //  ORDINARY — Capacity Range Query
        // ================================================================

        private CapacityRangeData GetOrdCapacityRange(SolarReportType rt, string typeCode,
            string calcCycle, List<string> tariffCodes, string netCond, int minCap, int maxCap)
        {
            var data = new CapacityRangeData();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string inClause = string.Join(",", tariffCodes.Select((_, i) => "?"));
                    string capacityCond = BuildCapacityCondition(minCap, maxCap);
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT COUNT(*), COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                  $"FROM netmtcons n, areas a " +
                                  $"WHERE {capacityCond} AND {netCond} AND tariff_code IN ({inClause}) " +
                                  $"AND n.calc_cycle=? AND schm='3' " +
                                  $"AND a.area_code=n.area_code AND a.prov_code=?";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT COUNT(*), COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                  $"FROM netmtcons n, areas a " +
                                  $"WHERE {capacityCond} AND {netCond} AND tariff_code IN ({inClause}) " +
                                  $"AND n.calc_cycle=? AND schm='3' " +
                                  $"AND a.area_code=n.area_code AND a.region=?";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        default: // EntireCEB
                            sql = $"SELECT COUNT(*), COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                  $"FROM netmtcons n, areas a " +
                                  $"WHERE {capacityCond} AND {netCond} AND n.tariff_code IN ({inClause}) " +
                                  $"AND n.calc_cycle=? AND n.schm='3' AND a.area_code=n.area_code";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                        if (reader.Read())
                        {
                            data.NoOfCustomers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            data.KwhUnits = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                            data.PaidAmount = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdCapacityRange min={minCap} max={maxCap}");
            }
            return data;
        }

        // ================================================================
        //  BULK — Capacity Range Query
        // ================================================================

        private CapacityRangeData GetBulkCapacityRange(SolarReportType rt, string typeCode,
            string billCycle, List<string> tariffCodes, string netType, int minCap, int maxCap)
        {
            var data = new CapacityRangeData();
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string inClause = string.Join(",", tariffCodes.Select((_, i) => "?"));
                    string capacityCond = BuildCapacityCondition(minCap, maxCap);
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT COUNT(*), COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                  $"FROM netmtcons n, netmeter m, areas a " +
                                  $"WHERE {capacityCond} AND n.net_type=? AND n.bill_cycle=? " +
                                  $"AND m.schm='3' AND m.acc_nbr=n.acc_nbr " +
                                  $"AND a.area_code=n.area_cd AND a.prov_code=? AND tariff IN ({inClause})";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT COUNT(*), COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                  $"FROM netmtcons n, netmeter m, areas a " +
                                  $"WHERE {capacityCond} AND n.net_type=? AND n.bill_cycle=? " +
                                  $"AND m.schm='3' AND m.acc_nbr=n.acc_nbr " +
                                  $"AND a.area_code=n.area_cd AND a.region=? AND tariff IN ({inClause})";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;

                        default: // EntireCEB
                            sql = $"SELECT COUNT(*), COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                  $"FROM netmtcons n, netmeter m, areas a " +
                                  $"WHERE {capacityCond} AND n.net_type=? AND n.bill_cycle=? " +
                                  $"AND m.schm='3' AND m.acc_nbr=n.acc_nbr " +
                                  $"AND a.area_code=n.area_cd AND tariff IN ({inClause})";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", billCycle);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                        if (reader.Read())
                        {
                            data.NoOfCustomers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            data.KwhUnits = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                            data.PaidAmount = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkCapacityRange min={minCap} max={maxCap}");
            }
            return data;
        }

        // ================================================================
        //  UTILITY
        // ================================================================

        /// <summary>
        /// Builds the capacity condition for SQL WHERE clause.
        /// minCap=0, maxCap=20 → "(gen_cap > 0 AND gen_cap <= 20)"
        /// minCap=20, maxCap=100 → "(gen_cap > 20 AND gen_cap <= 100)"
        /// minCap=500, maxCap=0 → "gen_cap > 500"
        /// </summary>
        private string BuildCapacityCondition(int minCap, int maxCap)
        {
            if (maxCap == 0)
            {
                // Above minCap (e.g., > 500)
                return $"n.gen_cap > {minCap}";
            }
            else
            {
                // Between minCap and maxCap (e.g., > 0 AND <= 20)
                return $"(n.gen_cap > {minCap} AND n.gen_cap <= {maxCap})";
            }
        }

        private string MonthNameToNumber(string monthName)
        {
            switch ((monthName ?? "").Trim().ToLower())
            {
                case "jan": return "1";
                case "feb": return "2";
                case "mar": return "3";
                case "apr": return "4";
                case "may": return "5";
                case "jun": return "6";
                case "jul": return "7";
                case "aug": return "8";
                case "sep": return "9";
                case "oct": return "10";
                case "nov": return "11";
                case "dec": return "12";
                default: return monthName;
            }
        }

        private SolarReportType MapReportType(PUCSLReportCategory cat)
        {
            switch (cat)
            {
                case PUCSLReportCategory.Province: return SolarReportType.Province;
                case PUCSLReportCategory.Region: return SolarReportType.Region;
                default: return SolarReportType.EntireCEB;
            }
        }

        // ================================================================
        //  HELPER CLASSES
        // ================================================================

        private class TariffCategoryItem
        {
            public string TariffCat { get; set; }
            public string TariffCatDisplay { get; set; }
        }

        private class CapacityRangeData
        {
            public int NoOfCustomers { get; set; }
            public decimal KwhUnits { get; set; }
            public decimal PaidAmount { get; set; }
        }
    }
}