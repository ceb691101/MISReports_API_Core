using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.PUCSLReports.PUCSLSolarConnection
{
    /// <summary>
    /// DAO for PUCSL Raw Data for Solar Report (Report 4).
    /// 
    /// Supports all 4 net types filtered by solarType parameter:
    /// - NetMetering (net_type='1')
    /// - NetAccounting (net_type='2' OR net_type='5')
    /// - NetPlus (net_type='3')
    /// - NetPlusPlus (net_type='4')
    ///
    /// Returns Import/Export with Peak/Off-Peak/Day breakdown for Bulk.
    /// Ordinary only returns Day totals (Peak/Off-Peak are 0).
    ///
    /// Ordinary data -> InformixConnection     (GetConnection(false))
    /// Bulk data     -> InformixBulkConnection (GetConnection(true))
    /// </summary>
    public class RawDataForSolarDao
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
        public RawDataForSolarResponse GetRawDataForSolarReport(PUCSLRequest request)
        {
            var response = new RawDataForSolarResponse
            {
                Ordinary = new List<RawSolarData>(),
                Bulk = new List<RawSolarData>()
            };

            try
            {
                logger.Info("=== START GetRawDataForSolarReport ===");
                logger.Info($"Category={request.ReportCategory}, TypeCode={request.TypeCode}, " +
                            $"BillCycle={request.BillCycle}, SolarType={request.SolarType}");

                SolarReportType reportType = MapReportType(request.ReportCategory);

                // Get net_type condition based on solarType
                var (netTypeCondition, netTypeValue, isNetAccounting) = GetNetTypeCondition(request.SolarType);

                // Get Year and Month from BillCycle
                var (year, month) = GetYearMonthFromCycle(request.BillCycle);

                // ═══════════════════════════════════════════════════════════
                //  ORDINARY SECTION
                // ═══════════════════════════════════════════════════════════

                // All net types: Get all tariff classes in ONE query (no loop needed)
                var allOrdinaryData = GetAllOrdinaryData(reportType, request.TypeCode,
                    request.BillCycle, year, month, netTypeCondition, netTypeValue, isNetAccounting);
                response.Ordinary.AddRange(allOrdinaryData);

                // GV1 for ALL net types (GP-3 and GP-4)
                var gv1Ord = GetOrdinaryGV1Data(reportType, request.TypeCode,
                    request.BillCycle, year, month, netTypeCondition, netTypeValue, isNetAccounting);
                if (gv1Ord != null)
                {
                    response.Ordinary.Add(gv1Ord);
                }

                // Calculate Ordinary Total
                var ordinaryTotal = new RawSolarData
                {
                    Category = "Total",
                    Year = year,
                    Month = month
                };

                foreach (var ord in response.Ordinary)
                {
                    ordinaryTotal.ImportDay += ord.ImportDay;
                    ordinaryTotal.ImportPeak += ord.ImportPeak;
                    ordinaryTotal.ImportOffPeak += ord.ImportOffPeak;
                    ordinaryTotal.ExportDay += ord.ExportDay;
                    ordinaryTotal.ExportPeak += ord.ExportPeak;
                    ordinaryTotal.ExportOffPeak += ord.ExportOffPeak;
                    ordinaryTotal.BroughtForwardKwh += ord.BroughtForwardKwh;
                    ordinaryTotal.CarryForwardKwh += ord.CarryForwardKwh;
                }

                response.OrdinaryTotal = ordinaryTotal;

                // ═══════════════════════════════════════════════════════════
                //  BULK SECTION
                // ═══════════════════════════════════════════════════════════

                string bulkTypeCode = request.TypeCode;
                if (reportType == SolarReportType.Province && !string.IsNullOrEmpty(request.TypeCode) && request.TypeCode.Length == 1)
                {
                    bulkTypeCode = request.TypeCode.PadLeft(2, '0');
                }

                var bulkData = GetBulkData(reportType, bulkTypeCode, request.BillCycle,
                    year, month, netTypeValue, isNetAccounting);
                response.Bulk = bulkData;

                // Calculate Bulk Total
                var bulkTotal = new RawSolarData
                {
                    Category = "Total",
                    Year = year,
                    Month = month
                };

                foreach (var bulk in response.Bulk)
                {
                    bulkTotal.ImportDay += bulk.ImportDay;
                    bulkTotal.ImportPeak += bulk.ImportPeak;
                    bulkTotal.ImportOffPeak += bulk.ImportOffPeak;
                    bulkTotal.ExportDay += bulk.ExportDay;
                    bulkTotal.ExportPeak += bulk.ExportPeak;
                    bulkTotal.ExportOffPeak += bulk.ExportOffPeak;
                    bulkTotal.BroughtForwardKwh += bulk.BroughtForwardKwh;
                    bulkTotal.CarryForwardKwh += bulk.CarryForwardKwh;
                }

                response.BulkTotal = bulkTotal;

                response.ErrorMessage = string.Empty;
                logger.Info($"=== END GetRawDataForSolarReport — Ordinary: {response.Ordinary.Count}, Bulk: {response.Bulk.Count} ===");
                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetRawDataForSolarReport");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        // ================================================================
        //  NET TYPE MAPPING
        // ================================================================

        /// <summary>
        /// Maps SolarNetType to SQL condition and net_type value.
        /// Returns: (condition string, net_type value, isNetAccounting flag)
        /// </summary>
        private (string condition, string netTypeValue, bool isNetAccounting) GetNetTypeCondition(SolarNetType solarType)
        {
            switch (solarType)
            {
                case SolarNetType.NetMetering:
                    return ("net_type='1'", "1", false);

                case SolarNetType.NetAccounting:
                    return ("(net_type='2' OR net_type='5')", "2", true);

                case SolarNetType.NetPlus:
                    return ("net_type='3'", "3", false);

                case SolarNetType.NetPlusPlus:
                    return ("net_type='4'", "4", false);

                default:
                    return ("net_type='1'", "1", false);
            }
        }

        // ================================================================
        //  TARIFF CLASSES
        // ================================================================

        // ================================================================
        //  ORDINARY — Get All Classes (Unified for All Net Types)
        // ================================================================

        /// <summary>
        /// Gets all ordinary tariff classes in a single query.
        /// Handles both Net Metering (with bf/cf) and Net Accounting/Plus/PlusPlus (without bf/cf).
        /// </summary>
        /// <summary>
        /// Gets all ordinary tariff classes in a single query.
        /// Handles both Net Metering (with bf/cf) and Net Accounting/Plus/PlusPlus (without bf/cf).
        /// </summary>
        private List<RawSolarData> GetAllOrdinaryData(SolarReportType rt, string typeCode,
            string calcCycle, string year, string month, string netTypeCondition,
            string netTypeValue, bool isNetAccounting)
        {
            var dataList = new List<RawSolarData>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    // Determine if this is Net Metering (includes bf/cf columns)
                    bool isNetMetering = (netTypeValue == "1");

                    if (isNetMetering)
                    {
                        // Net Metering: Include bf_units and cf_units (4 value columns)
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = $"SELECT tariff_class, COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp, COALESCE(SUM(bf_units),0) AS bf, " +
                                      $"COALESCE(SUM(cf_units),0) AS cf " +
                                      $"FROM netmtcons n, tariff_code t, areas a " +
                                      $"WHERE n.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND n.tariff_code=t.tariff_code " +
                                      $"AND a.area_code=n.area_code AND a.prov_code=? " +
                                      $"AND tariff_class NOT IN ('GV1UV','GV1SH') " +
                                      $"GROUP BY 1 ORDER BY 1";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            case SolarReportType.Region:
                                sql = $"SELECT tariff_class, COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp, COALESCE(SUM(bf_units),0) AS bf, " +
                                      $"COALESCE(SUM(cf_units),0) AS cf " +
                                      $"FROM netmtcons n, tariff_code t, areas a " +
                                      $"WHERE n.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND n.tariff_code=t.tariff_code " +
                                      $"AND a.area_code=n.area_code AND a.region=? " +
                                      $"AND tariff_class NOT IN ('GV1UV','GV1SH') " +
                                      $"GROUP BY 1 ORDER BY 1";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            default: // EntireCEB
                                sql = $"SELECT tariff_class, COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp, COALESCE(SUM(bf_units),0) AS bf, " +
                                      $"COALESCE(SUM(cf_units),0) AS cf " +
                                      $"FROM netmtcons n, tariff_code t " +
                                      $"WHERE n.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND n.tariff_code=t.tariff_code " +
                                      $"AND tariff_class NOT IN ('GV1UV','GV1SH') " +
                                      $"GROUP BY 1 ORDER BY 1";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                break;
                        }

                        using (cmd)
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                            {
                                dataList.Add(new RawSolarData
                                {
                                    Category = reader[0]?.ToString().Trim() ?? "",
                                    Year = year,
                                    Month = month,
                                    ImportDay = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]),  // units_in = exp
                                    ImportPeak = 0,
                                    ImportOffPeak = 0,
                                    ExportDay = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]),  // units_out = imp
                                    ExportPeak = 0,
                                    ExportOffPeak = 0,
                                    BroughtForwardKwh = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]),  // bf_units
                                    CarryForwardKwh = reader[4] == DBNull.Value ? 0 : Convert.ToDecimal(reader[4])     // cf_units
                                });
                            }
                    }
                    else
                    {
                        // Net Accounting/Plus/PlusPlus: NO bf_units, cf_units (2 value columns only)
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = $"SELECT tariff_class, COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp " +
                                      $"FROM netmtcons n, tariff_code t, areas a " +
                                      $"WHERE n.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND n.tariff_code=t.tariff_code " +
                                      $"AND a.area_code=n.area_code AND a.prov_code=? " +
                                      $"AND tariff_class NOT IN ('GV1UV','GV1SH') " +
                                      $"GROUP BY 1 ORDER BY 1";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            case SolarReportType.Region:
                                sql = $"SELECT tariff_class, COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp " +
                                      $"FROM netmtcons n, tariff_code t, areas a " +
                                      $"WHERE n.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND n.tariff_code=t.tariff_code " +
                                      $"AND a.area_code=n.area_code AND a.region=? " +
                                      $"AND tariff_class NOT IN ('GV1UV','GV1SH') " +
                                      $"GROUP BY 1 ORDER BY 1";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            default: // EntireCEB
                                sql = $"SELECT tariff_class, COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp " +
                                      $"FROM netmtcons n, tariff_code t " +
                                      $"WHERE n.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND n.tariff_code=t.tariff_code " +
                                      $"AND tariff_class NOT IN ('GV1UV','GV1SH') " +
                                      $"GROUP BY 1 ORDER BY 1";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                break;
                        }

                        using (cmd)
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                            {
                                dataList.Add(new RawSolarData
                                {
                                    Category = reader[0]?.ToString().Trim() ?? "",
                                    Year = year,
                                    Month = month,
                                    ImportDay = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]),  // units_in = exp
                                    ImportPeak = 0,
                                    ImportOffPeak = 0,
                                    ExportDay = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]),  // units_out = imp
                                    ExportPeak = 0,
                                    ExportOffPeak = 0,
                                    BroughtForwardKwh = 0,  // Always 0 for non-NetMetering
                                    CarryForwardKwh = 0     // Always 0 for non-NetMetering
                                });
                            }
                    }
                }
                logger.Info($"GetAllOrdinaryData: Found {dataList.Count} tariff classes");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAllOrdinaryData EXCEPTION");
            }
            return dataList;
        }

        // ================================================================
        //  ORDINARY — GV1 Special Handling (GP-3 and GP-4)
        // ================================================================

        private RawSolarData GetOrdinaryGV1Data(SolarReportType rt, string typeCode,
            string calcCycle, string year, string month, string netTypeCondition,
            string netTypeValue, bool isNetAccounting)
        {
            RawSolarData data = null;
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    // CRITICAL: bf_units and cf_units are ONLY for Net Metering (net_type='1')
                    bool isNetMetering = (netTypeValue == "1");

                    if (isNetMetering)
                    {
                        // Net Metering: Include bf_units and cf_units
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = $"SELECT 'GV1', COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp, COALESCE(SUM(bf_units),0) AS bf, " +
                                      $"COALESCE(SUM(cf_units),0) AS cf " +
                                      $"FROM netmtcons a, tariff_code c, areas r " +
                                      $"WHERE a.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND a.tariff_code=c.tariff_code " +
                                      $"AND tariff_type IN ('GP-3','GP-4') " +
                                      $"AND r.area_code=a.area_code AND r.prov_code=?";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            case SolarReportType.Region:
                                sql = $"SELECT 'GV1', COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp, COALESCE(SUM(bf_units),0) AS bf, " +
                                      $"COALESCE(SUM(cf_units),0) AS cf " +
                                      $"FROM netmtcons a, tariff_code c, areas r " +
                                      $"WHERE a.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND a.tariff_code=c.tariff_code " +
                                      $"AND tariff_type IN ('GP-3','GP-4') " +
                                      $"AND r.area_code=a.area_code AND r.region=?";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            default: // EntireCEB
                                sql = $"SELECT 'GV1', COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp, COALESCE(SUM(bf_units),0) AS bf, " +
                                      $"COALESCE(SUM(cf_units),0) AS cf " +
                                      $"FROM netmtcons a, tariff_code c " +
                                      $"WHERE a.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND a.tariff_code=c.tariff_code " +
                                      $"AND tariff_type IN ('GP-3','GP-4')";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                break;
                        }

                        using (cmd)
                        using (var reader = cmd.ExecuteReader())
                            if (reader.Read())
                            {
                                data = new RawSolarData
                                {
                                    Category = "GV1",
                                    Year = year,
                                    Month = month,
                                    ImportDay = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]),  // units_in = exp
                                    ImportPeak = 0,
                                    ImportOffPeak = 0,
                                    ExportDay = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]),  // units_out = imp
                                    ExportPeak = 0,
                                    ExportOffPeak = 0,
                                    BroughtForwardKwh = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]),
                                    CarryForwardKwh = reader[4] == DBNull.Value ? 0 : Convert.ToDecimal(reader[4])
                                };
                            }
                    }
                    else
                    {
                        // Net Accounting/Plus/PlusPlus: NO bf_units and cf_units
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = $"SELECT 'GV1', COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp " +
                                      $"FROM netmtcons a, tariff_code c, areas r " +
                                      $"WHERE a.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND a.tariff_code=c.tariff_code " +
                                      $"AND tariff_type IN ('GP-3','GP-4') " +
                                      $"AND r.area_code=a.area_code AND r.prov_code=?";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            case SolarReportType.Region:
                                sql = $"SELECT 'GV1', COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp " +
                                      $"FROM netmtcons a, tariff_code c, areas r " +
                                      $"WHERE a.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND a.tariff_code=c.tariff_code " +
                                      $"AND tariff_type IN ('GP-3','GP-4') " +
                                      $"AND r.area_code=a.area_code AND r.region=?";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            default: // EntireCEB
                                sql = $"SELECT 'GV1', COALESCE(SUM(units_in),0) AS exp, " +
                                      $"COALESCE(SUM(units_out),0) AS imp " +
                                      $"FROM netmtcons a, tariff_code c " +
                                      $"WHERE a.calc_cycle=? AND {netTypeCondition} " +
                                      $"AND a.tariff_code=c.tariff_code " +
                                      $"AND tariff_type IN ('GP-3','GP-4')";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", calcCycle);
                                break;
                        }

                        using (cmd)
                        using (var reader = cmd.ExecuteReader())
                            if (reader.Read())
                            {
                                data = new RawSolarData
                                {
                                    Category = "GV1",
                                    Year = year,
                                    Month = month,
                                    ImportDay = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]),  // units_in = exp
                                    ImportPeak = 0,
                                    ImportOffPeak = 0,
                                    ExportDay = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]),  // units_out = imp
                                    ExportPeak = 0,
                                    ExportOffPeak = 0,
                                    BroughtForwardKwh = 0,  // Always 0 for non-NetMetering
                                    CarryForwardKwh = 0     // Always 0 for non-NetMetering
                                };
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetOrdinaryGV1Data EXCEPTION");
            }
            return data;
        }

        // ================================================================
        //  BULK — Get All Data
        // ================================================================

        private List<RawSolarData> GetBulkData(SolarReportType rt, string typeCode,
            string billCycle, string year, string month, string netTypeValue, bool isNetAccounting)
        {
            var dataList = new List<RawSolarData>();
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    // CRITICAL: bf_units and cf_units are ONLY for Net Metering (net_type='1')
                    bool isNetMetering = (netTypeValue == "1");

                    if (isNetMetering)
                    {
                        // Net Metering: Include bf_units and cf_units (8 columns total)
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = "SELECT tariff, " +
                                      "COALESCE(SUM(imp_kwd_units),0), COALESCE(SUM(imp_kwp_units),0), " +
                                      "COALESCE(SUM(imp_kwo_units),0), COALESCE(SUM(exp_kwd_units),0), " +
                                      "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0), " +
                                      "COALESCE(SUM(bf_units),0), COALESCE(SUM(cf_units),0) " +
                                      "FROM netmtcons n, areas a " +
                                      "WHERE bill_cycle=? AND net_type='1' " +
                                      "AND a.area_code=n.area_cd AND a.prov_code=? " +
                                      "GROUP BY tariff ORDER BY tariff ASC";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            case SolarReportType.Region:
                                sql = "SELECT tariff, " +
                                      "COALESCE(SUM(imp_kwd_units),0), COALESCE(SUM(imp_kwp_units),0), " +
                                      "COALESCE(SUM(imp_kwo_units),0), COALESCE(SUM(exp_kwd_units),0), " +
                                      "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0), " +
                                      "COALESCE(SUM(bf_units),0), COALESCE(SUM(cf_units),0) " +
                                      "FROM netmtcons n, areas a " +
                                      "WHERE bill_cycle=? AND net_type='1' " +
                                      "AND a.area_code=n.area_cd AND a.region=? " +
                                      "GROUP BY tariff ORDER BY tariff ASC";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            default: // EntireCEB
                                sql = "SELECT tariff, " +
                                      "COALESCE(SUM(imp_kwd_units),0), COALESCE(SUM(imp_kwp_units),0), " +
                                      "COALESCE(SUM(imp_kwo_units),0), COALESCE(SUM(exp_kwd_units),0), " +
                                      "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0), " +
                                      "COALESCE(SUM(bf_units),0), COALESCE(SUM(cf_units),0) " +
                                      "FROM netmtcons " +
                                      "WHERE bill_cycle=? AND net_type='1' " +
                                      "GROUP BY tariff ORDER BY tariff ASC";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                break;
                        }

                        using (cmd)
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                            {
                                dataList.Add(new RawSolarData
                                {
                                    Category = reader[0]?.ToString().Trim() ?? "",
                                    Year = year,
                                    Month = month,
                                    ImportDay = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]),
                                    ImportPeak = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]),
                                    ImportOffPeak = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]),
                                    ExportDay = reader[4] == DBNull.Value ? 0 : Convert.ToDecimal(reader[4]),
                                    ExportPeak = reader[5] == DBNull.Value ? 0 : Convert.ToDecimal(reader[5]),
                                    ExportOffPeak = reader[6] == DBNull.Value ? 0 : Convert.ToDecimal(reader[6]),
                                    BroughtForwardKwh = reader[7] == DBNull.Value ? 0 : Convert.ToDecimal(reader[7]),
                                    CarryForwardKwh = reader[8] == DBNull.Value ? 0 : Convert.ToDecimal(reader[8])
                                });
                            }
                    }
                    else
                    {
                        // Net Accounting/Plus/PlusPlus: NO bf_units and cf_units (6 columns only)
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = "SELECT tariff, " +
                                      "COALESCE(SUM(imp_kwd_units),0), COALESCE(SUM(imp_kwp_units),0), " +
                                      "COALESCE(SUM(imp_kwo_units),0), COALESCE(SUM(exp_kwd_units),0), " +
                                      "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0) " +
                                      "FROM netmtcons n, areas a " +
                                      "WHERE bill_cycle=? AND net_type=? " +
                                      "AND a.area_code=n.area_cd AND a.prov_code=? " +
                                      "GROUP BY tariff ORDER BY tariff ASC";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", netTypeValue);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            case SolarReportType.Region:
                                sql = "SELECT tariff, " +
                                      "COALESCE(SUM(imp_kwd_units),0), COALESCE(SUM(imp_kwp_units),0), " +
                                      "COALESCE(SUM(imp_kwo_units),0), COALESCE(SUM(exp_kwd_units),0), " +
                                      "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0) " +
                                      "FROM netmtcons n, areas a " +
                                      "WHERE bill_cycle=? AND net_type=? " +
                                      "AND a.area_code=n.area_cd AND a.region=? " +
                                      "GROUP BY tariff ORDER BY tariff ASC";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", netTypeValue);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                break;

                            default: // EntireCEB
                                sql = "SELECT tariff, " +
                                      "COALESCE(SUM(imp_kwd_units),0), COALESCE(SUM(imp_kwp_units),0), " +
                                      "COALESCE(SUM(imp_kwo_units),0), COALESCE(SUM(exp_kwd_units),0), " +
                                      "COALESCE(SUM(exp_kwp_units),0), COALESCE(SUM(exp_kwo_units),0) " +
                                      "FROM netmtcons " +
                                      "WHERE bill_cycle=? AND net_type=? " +
                                      "GROUP BY tariff ORDER BY tariff ASC";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", netTypeValue);
                                break;
                        }

                        using (cmd)
                        using (var reader = cmd.ExecuteReader())
                            while (reader.Read())
                            {
                                dataList.Add(new RawSolarData
                                {
                                    Category = reader[0]?.ToString().Trim() ?? "",
                                    Year = year,
                                    Month = month,
                                    ImportDay = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]),
                                    ImportPeak = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]),
                                    ImportOffPeak = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]),
                                    ExportDay = reader[4] == DBNull.Value ? 0 : Convert.ToDecimal(reader[4]),
                                    ExportPeak = reader[5] == DBNull.Value ? 0 : Convert.ToDecimal(reader[5]),
                                    ExportOffPeak = reader[6] == DBNull.Value ? 0 : Convert.ToDecimal(reader[6]),
                                    BroughtForwardKwh = 0,  // Always 0 for non-NetMetering
                                    CarryForwardKwh = 0     // Always 0 for non-NetMetering
                                });
                            }
                    }
                }
                logger.Info($"GetBulkData: Found {dataList.Count} bulk tariffs");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetBulkData EXCEPTION");
            }
            return dataList;
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
        /// Example: "440" -> "Apr 25" -> Year="25", Month="Apr"
        /// </summary>
        private (string year, string month) GetYearMonthFromCycle(string billCycle)
        {
            try
            {
                int cycle = int.Parse(billCycle);
                string monthYear = BillCycleHelper.ConvertToMonthYear(cycle); // Returns "Apr 25"

                if (string.IsNullOrEmpty(monthYear) || monthYear == "Invalid" || monthYear == "Unknown")
                {
                    logger.Warn($"Invalid bill cycle: {billCycle}");
                    return ("", "");
                }

                // Parse "Apr 25" into month number and year
                var parts = monthYear.Split(' ');
                if (parts.Length == 2)
                {
                    string monthName = parts[0];
                    string year = parts[1];

                    // Convert month name to month number
                    string monthNumber = ConvertMonthNameToNumber(monthName);

                    return (year, monthNumber); // year="25", month="4"
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
        /// Converts month name (e.g., "Apr", "Jan") to month number (e.g., "4", "1")
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
}