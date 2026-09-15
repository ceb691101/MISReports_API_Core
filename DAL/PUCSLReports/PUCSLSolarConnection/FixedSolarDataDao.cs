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
    /// DAO for PUCSL Fixed Solar Data Submission Report (Report 1 from PDF).
    ///
    /// Ordinary data -> InformixConnection     (GetConnection(false))
    /// Bulk data     -> InformixBulkConnection (GetConnection(true))
    ///
    /// Net-type SQL conditions applied from SolarNetType input:
    ///   NetAccounting -> (net_type='2' OR net_type='5')
    ///   NetPlus       -> net_type='3'
    ///   NetPlusPlus   -> net_type='4'
    ///
    /// IMPORTANT: Each tariff category can have MULTIPLE tariff codes.
    /// All codes for a category are retrieved and queried together using IN clause.
    /// SQL aggregation (SUM, COUNT) automatically combines data across all codes.
    /// </summary>
    public class FixedSolarDataDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string RateIn = "('15.50','22','34.50','37','23.18','27.06')";
        private const string RateNotIn = "('15.50','22','34.50','37','23.18','27.06')";

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestAllConnections(out errorMessage);
        }

        // ================================================================
        //  PUBLIC ENTRY POINT
        // ================================================================
        public List<FixedSolarDataModel> GetFixedSolarDataReport(PUCSLRequest request)
        {
            var results = new List<FixedSolarDataModel>();

            try
            {
                logger.Info("=== START GetFixedSolarDataReport ===");
                logger.Info($"Category={request.ReportCategory}, TypeCode={request.TypeCode}, " +
                            $"BillCycle={request.BillCycle}, SolarType={request.SolarType}");

                // Throw exception for invalid solar type
                if (request.SolarType == SolarNetType.NetMetering)
                {
                    logger.Warn("NetMetering is not supported for Fixed Solar Data report");
                    throw new ArgumentException("NetMetering is not supported for Fixed Solar Data report. Please use NetAccounting, NetPlus, or NetPlusPlus.");
                }

                SolarReportType reportType = MapReportType(request.ReportCategory);

                // Ordinary: inline SQL fragment  e.g. "(net_type='2' OR net_type='5')"
                string ordNetCond = GetOrdNetTypeCondition(request.SolarType);

                // Bulk: parameterised  (netType2 only used for NetAccounting)
                bool bulkIsDouble = request.SolarType == SolarNetType.NetAccounting;
                string bulkNetType1 = GetBulkNetType1(request.SolarType);
                string bulkNetType2 = GetBulkNetType2(request.SolarType);

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
                    var model = new FixedSolarDataModel
                    {
                        Category = tc.TariffCatDisplay,
                        Year = rowYear,
                        Month = rowMonth
                    };

                    // ── ORDINARY ─────────────────────────────────────────────
                    var ordCodes = GetOrdTariffCodes(tc.TariffCat);
                    if (ordCodes != null && ordCodes.Count > 0)
                    {
                        model.OrdinaryNoOfCustomers = GetOrdAccountCount(
                            reportType, request.TypeCode, request.BillCycle, ordCodes, ordNetCond);

                        var ordRates = GetOrdSalesByTrackedRates(
                            reportType, request.TypeCode, request.BillCycle, ordCodes, ordNetCond);

                        model.OrdinaryKwhAt1550 = GetUnits(ordRates, "15.50");
                        model.OrdinaryKwhAt22 = GetUnits(ordRates, "22");
                        model.OrdinaryKwhAt3450 = GetUnits(ordRates, "34.50");
                        model.OrdinaryKwhAt37 = GetUnits(ordRates, "37");
                        model.OrdinaryKwhAt2318 = GetUnits(ordRates, "23.18");
                        model.OrdinaryKwhAt2706 = GetUnits(ordRates, "27.06");

                        decimal ordPaid = 0;
                        foreach (var kv in ordRates) ordPaid += kv.Value.KwhSales;

                        var ordOther = GetOrdOtherSales(
                            reportType, request.TypeCode, request.BillCycle, ordCodes, ordNetCond);
                        model.OrdinaryKwhOthers = ordOther.UnitSale;
                        ordPaid += ordOther.KwhSales;
                        model.PaidAmount += ordPaid;
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

                        model.BulkNoOfCustomers = GetBulkAccountCount(
                            reportType, bulkTypeCode, request.BillCycle,
                            bulkCodes, bulkNetType1, bulkNetType2, bulkIsDouble);

                        var bulkRates = GetBulkSalesByTrackedRates(
                            reportType, bulkTypeCode, request.BillCycle,
                            bulkCodes, bulkNetType1, bulkNetType2, bulkIsDouble);

                        model.BulkKwhAt1550 = GetUnits(bulkRates, "15.50");
                        model.BulkKwhAt22 = GetUnits(bulkRates, "22");
                        model.BulkKwhAt3450 = GetUnits(bulkRates, "34.50");
                        model.BulkKwhAt37 = GetUnits(bulkRates, "37");
                        model.BulkKwhAt2318 = GetUnits(bulkRates, "23.18");
                        model.BulkKwhAt2706 = GetUnits(bulkRates, "27.06");

                        decimal bulkPaid = 0;
                        foreach (var kv in bulkRates) bulkPaid += kv.Value.KwhSales;
                        model.PaidAmount += bulkPaid;
                    }

                    // ── COMBINE Ordinary + Bulk ───────────────────────────────
                    model.NoOfCustomers = model.OrdinaryNoOfCustomers + model.BulkNoOfCustomers;
                    model.KwhAt1550 = model.OrdinaryKwhAt1550 + model.BulkKwhAt1550;
                    model.KwhAt22 = model.OrdinaryKwhAt22 + model.BulkKwhAt22;
                    model.KwhAt3450 = model.OrdinaryKwhAt3450 + model.BulkKwhAt3450;
                    model.KwhAt37 = model.OrdinaryKwhAt37 + model.BulkKwhAt37;
                    model.KwhAt2318 = model.OrdinaryKwhAt2318 + model.BulkKwhAt2318;
                    model.KwhAt2706 = model.OrdinaryKwhAt2706 + model.BulkKwhAt2706;
                    model.KwhOthers = model.OrdinaryKwhOthers;
                    model.ErrorMessage = string.Empty;
                    results.Add(model);
                }

                logger.Info($"=== END GetFixedSolarDataReport — {results.Count} rows ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetFixedSolarDataReport");
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

        private string GetBulkNetType1(SolarNetType st)
        {
            switch (st)
            {
                case SolarNetType.NetAccounting: return "2";
                case SolarNetType.NetPlus: return "3";
                case SolarNetType.NetPlusPlus: return "4";
                default: return "2";
            }
        }

        private string GetBulkNetType2(SolarNetType st)
            => st == SolarNetType.NetAccounting ? "5" : null;

        // ================================================================
        //  TARIFF CODES - Returns ALL codes for a category
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
                using (var conn = _dbConnection.GetConnection(false)) // FIXED: true for bulk DB
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
        //  ORDINARY — Account Count
        // ================================================================
        private int GetOrdAccountCount(SolarReportType rt, string typeCode,
            string calcCycle, List<string> tariffCodes, string netCond)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string inClause = string.Join(",", tariffCodes.Select((_, i) => "?"));
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT COUNT(n.acct_number) FROM netmtcons n, areas a " +
                                  $"WHERE {netCond} AND n.calc_cycle=? AND a.area_code=n.area_code " +
                                  $"AND a.prov_code=? AND n.tariff_code IN ({inClause}) AND (n.schm NOT IN ('3') OR n.schm IS NULL)";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT COUNT(n.acct_number) FROM netmtcons n, areas a " +
                                  $"WHERE {netCond} AND n.calc_cycle=? AND a.area_code=n.area_code " +
                                  $"AND a.region=? AND n.tariff_code IN ({inClause}) AND (n.schm NOT IN ('3') OR n.schm IS NULL)";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;

                        default: // EntireCEB
                            string nc = netCond.Replace("n.net_type", "net_type");
                            sql = $"SELECT COUNT(acct_number) FROM netmtcons " +
                                  $"WHERE {nc} AND calc_cycle=? AND tariff_code IN ({inClause}) " +
                                  $"AND (schm NOT IN ('3') OR schm IS NULL)";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;
                    }

                    using (cmd)
                    {
                        var v = cmd.ExecuteScalar();
                        return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
                    }
                }
            }
            catch (Exception ex) { logger.Error(ex, "GetOrdAccountCount"); return 0; }
        }

        // ================================================================
        //  ORDINARY — Sales at Tracked Rates
        // ================================================================
        private Dictionary<string, RateSalesRow> GetOrdSalesByTrackedRates(
            SolarReportType rt, string typeCode, string calcCycle,
            List<string> tariffCodes, string netCond)
        {
            var result = new Dictionary<string, RateSalesRow>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string inClause = string.Join(",", tariffCodes.Select((_, i) => "?"));
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT n.rate, COALESCE(SUM(kwh_sales),0), COALESCE(SUM(unitsale),0) " +
                                  $"FROM netmtcons n, areas a " +
                                  $"WHERE n.tariff_code IN ({inClause}) AND n.calc_cycle=? AND n.rate IN {RateIn} " +
                                  $"AND {netCond} AND (n.schm NOT IN ('3') OR n.schm IS NULL) " +
                                  $"AND a.area_code=n.area_code AND a.prov_code=? GROUP BY 1";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT n.rate, COALESCE(SUM(kwh_sales),0), COALESCE(SUM(unitsale),0) " +
                                  $"FROM netmtcons n, areas a " +
                                  $"WHERE n.tariff_code IN ({inClause}) AND n.calc_cycle=? AND n.rate IN {RateIn} " +
                                  $"AND {netCond} AND (n.schm NOT IN ('3') OR n.schm IS NULL) " +
                                  $"AND a.area_code=n.area_code AND a.region=? GROUP BY 1";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        default:
                            string nc = netCond.Replace("n.net_type", "net_type");
                            sql = $"SELECT rate, COALESCE(SUM(kwh_sales),0), COALESCE(SUM(unitsale),0) " +
                                  $"FROM netmtcons " +
                                  $"WHERE tariff_code IN ({inClause}) AND calc_cycle=? AND rate IN {RateIn} " +
                                  $"AND {nc} AND (schm NOT IN ('3') OR schm IS NULL) GROUP BY 1";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            string rawRate = reader[0]?.ToString().Trim();
                            string rate = NormaliseRate(rawRate);
                            decimal kwh = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                            decimal unit = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                            result[rate] = new RateSalesRow { KwhSales = kwh, UnitSale = unit };
                        }
                }
            }
            catch (Exception ex) { logger.Error(ex, "GetOrdSalesByTrackedRates"); }
            return result;
        }

        // ================================================================
        //  ORDINARY — Sales at "Other" Rates
        // ================================================================
        private RateSalesRow GetOrdOtherSales(SolarReportType rt, string typeCode,
            string calcCycle, List<string> tariffCodes, string netCond)
        {
            var row = new RateSalesRow();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string inClause = string.Join(",", tariffCodes.Select((_, i) => "?"));
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT COALESCE(SUM(n.kwh_sales),0), COALESCE(SUM(n.unitsale),0) " +
                                  $"FROM netmtcons n, areas a " +
                                  $"WHERE n.tariff_code IN ({inClause}) AND n.calc_cycle=? AND n.rate NOT IN {RateNotIn} " +
                                  $"AND {netCond} AND (n.schm NOT IN ('3') OR n.schm IS NULL) " +
                                  $"AND a.area_code=n.area_code AND a.prov_code=?";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT COALESCE(SUM(n.kwh_sales),0), COALESCE(SUM(n.unitsale),0) " +
                                  $"FROM netmtcons n, areas a " +
                                  $"WHERE n.tariff_code IN ({inClause}) AND n.calc_cycle=? AND n.rate NOT IN {RateNotIn} " +
                                  $"AND {netCond} AND (n.schm NOT IN ('3') OR n.schm IS NULL) " +
                                  $"AND a.area_code=n.area_code AND a.region=?";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        default:
                            string nc = netCond.Replace("n.net_type", "net_type");
                            sql = $"SELECT COALESCE(SUM(kwh_sales),0), COALESCE(SUM(unitsale),0) " +
                                  $"FROM netmtcons " +
                                  $"WHERE tariff_code IN ({inClause}) AND calc_cycle=? AND rate NOT IN {RateNotIn} " +
                                  $"AND {nc} AND (schm NOT IN ('3') OR schm IS NULL)";
                            cmd.CommandText = sql;
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                        if (reader.Read())
                        {
                            row.KwhSales = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]);
                            row.UnitSale = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                        }
                }
            }
            catch (Exception ex) { logger.Error(ex, "GetOrdOtherSales"); }
            return row;
        }

        // ================================================================
        //  BULK — Account Count
        // ================================================================
        private int GetBulkAccountCount(SolarReportType rt, string typeCode,
            string billCycle, List<string> tariffCodes,
            string netType1, string netType2, bool isDouble)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string inClause = string.Join(",", tariffCodes.Select((_, i) => "?"));
                    string netFrag = isDouble ? "(n.net_type=? OR n.net_type=?)" : "n.net_type=?";
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT COUNT(n.acc_nbr) FROM netmtcons n, areas a, netmeter m " +
                                  $"WHERE n.bill_cycle=? AND m.acc_nbr=n.acc_nbr AND {netFrag} " +
                                  $"AND a.area_code=n.area_cd AND a.prov_code=? AND m.schm IN ('1','2') AND tariff IN ({inClause})";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            AddBulkNetParams(cmd, netType1, netType2, isDouble);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT COUNT(n.acc_nbr) FROM netmtcons n, areas a, netmeter m " +
                                  $"WHERE n.bill_cycle=? AND m.acc_nbr=n.acc_nbr AND {netFrag} " +
                                  $"AND a.area_code=n.area_cd AND a.region=? AND m.schm IN ('1','2') AND tariff IN ({inClause})";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            AddBulkNetParams(cmd, netType1, netType2, isDouble);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;

                        default:
                            sql = $"SELECT COUNT(n.acc_nbr) FROM netmtcons n, netmeter m " +
                                  $"WHERE n.bill_cycle=? AND m.acc_nbr=n.acc_nbr AND {netFrag} " +
                                  $"AND m.schm IN ('1','2') AND tariff IN ({inClause})";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            AddBulkNetParams(cmd, netType1, netType2, isDouble);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            break;
                    }

                    using (cmd)
                    {
                        var v = cmd.ExecuteScalar();
                        return v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
                    }
                }
            }
            catch (Exception ex) { logger.Error(ex, "GetBulkAccountCount"); return 0; }
        }

        // ================================================================
        //  BULK — Sales at Tracked Rates
        // ================================================================
        private Dictionary<string, RateSalesRow> GetBulkSalesByTrackedRates(
            SolarReportType rt, string typeCode, string billCycle, List<string> tariffCodes,
            string netType1, string netType2, bool isDouble)
        {
            var result = new Dictionary<string, RateSalesRow>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string inClause = string.Join(",", tariffCodes.Select((_, i) => "?"));
                    string netFrag = isDouble ? "(n.net_type=? OR n.net_type=?)" : "n.net_type=?";
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT rate, COALESCE(SUM(kwh_sales),0), COALESCE(SUM(unitsale),0) " +
                                  $"FROM netmtcons n, areas a, netmeter m " +
                                  $"WHERE bill_cycle=? AND tariff IN ({inClause}) AND m.acc_nbr=n.acc_nbr " +
                                  $"AND m.schm IN ('1','2') AND {netFrag} " +
                                  $"AND rate IN {RateIn} " +
                                  $"AND a.area_code=n.area_cd AND a.prov_code=? GROUP BY 1 ORDER BY 1";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            AddBulkNetParams(cmd, netType1, netType2, isDouble);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT rate, COALESCE(SUM(kwh_sales),0), COALESCE(SUM(unitsale),0) " +
                                  $"FROM netmtcons n, areas a, netmeter m " +
                                  $"WHERE bill_cycle=? AND tariff IN ({inClause}) AND m.acc_nbr=n.acc_nbr " +
                                  $"AND m.schm IN ('1','2') AND {netFrag} " +
                                  $"AND rate IN {RateIn} " +
                                  $"AND a.area_code=n.area_cd AND a.region=? GROUP BY 1 ORDER BY 1";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            AddBulkNetParams(cmd, netType1, netType2, isDouble);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        default:
                            sql = $"SELECT rate, COALESCE(SUM(kwh_sales),0), COALESCE(SUM(unitsale),0) " +
                                  $"FROM netmtcons n, netmeter m " +
                                  $"WHERE bill_cycle=? AND tariff IN ({inClause}) AND m.acc_nbr=n.acc_nbr " +
                                  $"AND m.schm IN ('1','2') AND {netFrag} " +
                                  $"AND rate IN {RateIn} GROUP BY 1 ORDER BY 1";
                            cmd.CommandText = sql;
                            cmd.Parameters.AddWithValue("?", billCycle);
                            foreach (var code in tariffCodes) cmd.Parameters.AddWithValue("?", code);
                            AddBulkNetParams(cmd, netType1, netType2, isDouble);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rawRate = reader[0]?.ToString().Trim();
                            string rate = NormaliseRate(rawRate);
                            decimal kwh = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                            decimal unit = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                            result[rate] = new RateSalesRow { KwhSales = kwh, UnitSale = unit };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetBulkSalesByTrackedRates EXCEPTION");
            }
            return result;
        }

        // ================================================================
        //  UTILITY
        // ================================================================

        private void AddBulkNetParams(OleDbCommand cmd, string nt1, string nt2, bool isDouble)
        {
            cmd.Parameters.AddWithValue("?", nt1);
            if (isDouble) cmd.Parameters.AddWithValue("?", nt2);
        }

        private decimal GetUnits(Dictionary<string, RateSalesRow> dict, string key)
            => dict.TryGetValue(key, out var row) ? row.UnitSale : 0m;

        private string NormaliseRate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            if (!decimal.TryParse(raw, out decimal d)) return raw.Trim();

            string[] knownKeys = { "15.50", "22", "34.50", "37", "23.18", "27.06" };
            foreach (var k in knownKeys)
            {
                if (decimal.TryParse(k, out decimal kd) && kd == d)
                    return k;
            }
            return raw.Trim();
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

        private class TariffCategoryItem
        {
            public string TariffCat { get; set; }
            public string TariffCatDisplay { get; set; }
        }

        private class RateSalesRow
        {
            public decimal KwhSales { get; set; }
            public decimal UnitSale { get; set; }
        }
    }
}