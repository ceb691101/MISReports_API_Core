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
    /// DAO for PUCSL Total No of Solar Customers Report (Report 3 from PDF).
    /// 
    /// Returns separate Ordinary and Bulk sections.
    /// Groups data by tariff_class (ordinary) and tariff (bulk) with net_type breakdown:
    ///   - Net Metering: net_type='1'
    ///   - Net Accounting: net_type IN ('2','5')
    ///   - Net Plus: net_type='3'
    ///   - Net Plus Plus: net_type='4'
    ///
    /// Ordinary data -> InformixConnection     (GetConnection(false))
    /// Bulk data     -> InformixBulkConnection (GetConnection(true))
    /// </summary>
    public class TotalSolarCustomersDao
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
        public TotalSolarCustomersResponse GetTotalSolarCustomersReport(PUCSLRequest request)
        {
            var response = new TotalSolarCustomersResponse
            {
                Ordinary = new List<OrdinaryData>(),
                Bulk = new List<BulkData>()
            };

            try
            {
                logger.Info("=== START GetTotalSolarCustomersReport ===");
                logger.Info($"Category={request.ReportCategory}, TypeCode={request.TypeCode}, " +
                            $"BillCycle={request.BillCycle}");
                logger.Info("NOTE: SolarType parameter is ignored for this report - returns all net types");

                SolarReportType reportType = MapReportType(request.ReportCategory);

                // ═══════════════════════════════════════════════════════════
                //  ORDINARY SECTION
                // ═══════════════════════════════════════════════════════════

                var tariffClasses = GetTariffClasses(request.BillCycle);
                if (tariffClasses.Count == 0)
                {
                    logger.Warn("No tariff classes found.");
                }

                foreach (var tariffClass in tariffClasses)
                {
                    var ordData = new OrdinaryData
                    {
                        TariffCategory = tariffClass
                    };

                    // Net Metering (net_type='1')
                    var nm = GetOrdByNetType(reportType, request.TypeCode,
                        request.BillCycle, tariffClass, "1", false);
                    ordData.NetMeteringCustomers = nm.Customers;
                    ordData.NetMeteringUnits = nm.Units;

                    // Net Accounting (net_type='2' OR net_type='5')
                    var na = GetOrdByNetType(reportType, request.TypeCode,
                        request.BillCycle, tariffClass, "2", true);
                    ordData.NetAccountingCustomers = na.Customers;
                    ordData.NetAccountingUnits = na.Units;

                    // Net Plus (net_type='3')
                    var np = GetOrdByNetType(reportType, request.TypeCode,
                        request.BillCycle, tariffClass, "3", false);
                    ordData.NetPlusCustomers = np.Customers;
                    ordData.NetPlusUnits = np.Units;

                    // Net Plus Plus (net_type='4')
                    var npp = GetOrdByNetType(reportType, request.TypeCode,
                        request.BillCycle, tariffClass, "4", false);
                    ordData.NetPlusPlusCustomers = npp.Customers;
                    ordData.NetPlusPlusUnits = npp.Units;

                    response.Ordinary.Add(ordData);
                }

                // Special GV1 handling for Ordinary
                var gv1Ord = new OrdinaryData { TariffCategory = "GV1" };
                var gv1Nm = GetOrdGV1ByNetType(reportType, request.TypeCode, request.BillCycle, "1");
                gv1Ord.NetMeteringCustomers = gv1Nm.Customers;
                gv1Ord.NetMeteringUnits = gv1Nm.Units;

                var gv1Na = GetOrdGV1ByNetType(reportType, request.TypeCode, request.BillCycle, "2");
                gv1Ord.NetAccountingCustomers = gv1Na.Customers;
                gv1Ord.NetAccountingUnits = gv1Na.Units;

                var gv1Np = GetOrdGV1ByNetType(reportType, request.TypeCode, request.BillCycle, "3");
                gv1Ord.NetPlusCustomers = gv1Np.Customers;
                gv1Ord.NetPlusUnits = gv1Np.Units;

                var gv1Npp = GetOrdGV1ByNetType(reportType, request.TypeCode, request.BillCycle, "4");
                gv1Ord.NetPlusPlusCustomers = gv1Npp.Customers;
                gv1Ord.NetPlusPlusUnits = gv1Npp.Units;

                response.Ordinary.Add(gv1Ord);

                // ═══════════════════════════════════════════════════════════
                //  ORDINARY TOTALS
                // ═══════════════════════════════════════════════════════════

                var ordinaryTotal = new OrdinaryData
                {
                    TariffCategory = "Total"
                };

                foreach (var ord in response.Ordinary)
                {
                    ordinaryTotal.NetMeteringCustomers += ord.NetMeteringCustomers;
                    ordinaryTotal.NetMeteringUnits += ord.NetMeteringUnits;
                    ordinaryTotal.NetAccountingCustomers += ord.NetAccountingCustomers;
                    ordinaryTotal.NetAccountingUnits += ord.NetAccountingUnits;
                    ordinaryTotal.NetPlusCustomers += ord.NetPlusCustomers;
                    ordinaryTotal.NetPlusUnits += ord.NetPlusUnits;
                    ordinaryTotal.NetPlusPlusCustomers += ord.NetPlusPlusCustomers;
                    ordinaryTotal.NetPlusPlusUnits += ord.NetPlusPlusUnits;
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

                var bulkTariffs = GetAllBulkTariffs(request.BillCycle);
                logger.Info($"Found {bulkTariffs.Count} bulk tariffs");

                foreach (var bulkTariff in bulkTariffs)
                {
                    var bulkData = new BulkData
                    {
                        TariffCategory = bulkTariff
                    };

                    // Net Metering (net_type='1')
                    var bnm = GetBulkByNetType(reportType, bulkTypeCode,
                        request.BillCycle, new List<string> { bulkTariff }, "1");
                    bulkData.NetMeteringCustomers = bnm.Customers;
                    bulkData.NetMeteringUnits = bnm.Units;

                    // Net Accounting (net_type='2')
                    var bna = GetBulkByNetType(reportType, bulkTypeCode,
                        request.BillCycle, new List<string> { bulkTariff }, "2");
                    bulkData.NetAccountingCustomers = bna.Customers;
                    bulkData.NetAccountingUnits = bna.Units;

                    // Net Plus (net_type='3')
                    var bnp = GetBulkByNetType(reportType, bulkTypeCode,
                        request.BillCycle, new List<string> { bulkTariff }, "3");
                    bulkData.NetPlusCustomers = bnp.Customers;
                    bulkData.NetPlusUnits = bnp.Units;

                    // Net Plus Plus (net_type='4')
                    var bnpp = GetBulkByNetType(reportType, bulkTypeCode,
                        request.BillCycle, new List<string> { bulkTariff }, "4");
                    bulkData.NetPlusPlusCustomers = bnpp.Customers;
                    bulkData.NetPlusPlusUnits = bnpp.Units;

                    response.Bulk.Add(bulkData);
                }

                // ═══════════════════════════════════════════════════════════
                //  BULK TOTALS
                // ═══════════════════════════════════════════════════════════

                var bulkTotal = new BulkData
                {
                    TariffCategory = "Total"
                };

                foreach (var bulk in response.Bulk)
                {
                    bulkTotal.NetMeteringCustomers += bulk.NetMeteringCustomers;
                    bulkTotal.NetMeteringUnits += bulk.NetMeteringUnits;
                    bulkTotal.NetAccountingCustomers += bulk.NetAccountingCustomers;
                    bulkTotal.NetAccountingUnits += bulk.NetAccountingUnits;
                    bulkTotal.NetPlusCustomers += bulk.NetPlusCustomers;
                    bulkTotal.NetPlusUnits += bulk.NetPlusUnits;
                    bulkTotal.NetPlusPlusCustomers += bulk.NetPlusPlusCustomers;
                    bulkTotal.NetPlusPlusUnits += bulk.NetPlusPlusUnits;
                }

                response.BulkTotal = bulkTotal;

                response.ErrorMessage = string.Empty;
                logger.Info($"=== END GetTotalSolarCustomersReport — Ordinary: {response.Ordinary.Count}, Bulk: {response.Bulk.Count} ===");
                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetTotalSolarCustomersReport");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        // ================================================================
        //  TARIFF CLASSES
        // ================================================================

        /// <summary>
        /// Gets distinct tariff_class values from netmtcons for the given bill cycle.
        /// Excludes GV1UV and GV1SH classes as per PDF.
        /// </summary>
        private List<string> GetTariffClasses(string calcCycle)
        {
            var classes = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    const string sql =
                        "SELECT c.tariff_class FROM netmtcons a, tariff_code c " +
                        "WHERE a.calc_cycle=? AND a.net_type IN ('1','2','3','4','5') " +
                        "AND a.tariff_code=c.tariff_code " +
                        "AND tariff_class NOT IN ('GV1UV','GV1SH') " +
                        "GROUP BY c.tariff_class ORDER BY c.tariff_class";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", calcCycle);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var tariffClass = reader[0]?.ToString().Trim();
                                if (!string.IsNullOrEmpty(tariffClass))
                                {
                                    classes.Add(tariffClass);
                                }
                            }
                        }
                    }
                }
                logger.Info($"Found {classes.Count} tariff classes");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetTariffClasses EXCEPTION");
            }
            return classes;
        }

        // ================================================================
        //  ORDINARY — By Net Type
        // ================================================================

        private NetTypeData GetOrdByNetType(SolarReportType rt, string typeCode,
            string calcCycle, string tariffClass, string netType, bool isNetAccounting)
        {
            var data = new NetTypeData();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string netCond = isNetAccounting
                        ? "(n.net_type='2' OR n.net_type='5')"
                        : "n.net_type=?";

                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT t.tariff_class, COUNT(n.acct_number), COALESCE(SUM(n.units_out),0) " +
                                  $"FROM netmtcons n, tariff_code t, areas a " +
                                  $"WHERE {netCond} AND n.tariff_code=t.tariff_code " +
                                  $"AND n.calc_cycle=? AND tariff_class=? " +
                                  $"AND a.area_code=n.area_code AND a.prov_code=? " +
                                  $"GROUP BY 1 ORDER BY 1";
                            cmd.CommandText = sql;
                            if (!isNetAccounting) cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffClass);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT t.tariff_class, COUNT(n.acct_number), COALESCE(SUM(n.units_out),0) " +
                                  $"FROM netmtcons n, tariff_code t, areas a " +
                                  $"WHERE {netCond} AND n.tariff_code=t.tariff_code " +
                                  $"AND n.calc_cycle=? AND tariff_class=? " +
                                  $"AND a.area_code=n.area_code AND a.region=? " +
                                  $"GROUP BY 1 ORDER BY 1";
                            cmd.CommandText = sql;
                            if (!isNetAccounting) cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffClass);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        default: // EntireCEB
                            sql = $"SELECT t.tariff_class, COUNT(n.acct_number), COALESCE(SUM(n.units_out),0) " +
                                  $"FROM netmtcons n, tariff_code t " +
                                  $"WHERE {netCond} AND n.tariff_code=t.tariff_code " +
                                  $"AND n.calc_cycle=? AND tariff_class=? " +
                                  $"GROUP BY 1 ORDER BY 1";
                            cmd.CommandText = sql;
                            if (!isNetAccounting) cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffClass);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                        if (reader.Read())
                        {
                            data.Customers = reader[1] == DBNull.Value ? 0 : Convert.ToInt32(reader[1]);
                            data.Units = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdByNetType class={tariffClass} netType={netType}");
            }
            return data;
        }

        // ================================================================
        //  ORDINARY — GV1 Special Handling (GP-3 and GP-4)
        // ================================================================

        private NetTypeData GetOrdGV1ByNetType(SolarReportType rt, string typeCode,
            string calcCycle, string netType)
        {
            var data = new NetTypeData();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string netCond = (netType == "2")
                        ? "(n.net_type='2' OR n.net_type='5')"
                        : "n.net_type=?";

                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            sql = $"SELECT 'GV1', COUNT(n.acct_number), COALESCE(SUM(n.units_out),0) " +
                                  $"FROM netmtcons n, tariff_code c, areas a " +
                                  $"WHERE {netCond} AND n.tariff_code=c.tariff_code " +
                                  $"AND n.calc_cycle=? AND tariff_type IN ('GP-3','GP-4') " +
                                  $"AND a.area_code=n.area_code AND a.prov_code=?";
                            cmd.CommandText = sql;
                            if (netType != "2") cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        case SolarReportType.Region:
                            sql = $"SELECT 'GV1', COUNT(n.acct_number), COALESCE(SUM(n.units_out),0) " +
                                  $"FROM netmtcons n, tariff_code c, areas a " +
                                  $"WHERE {netCond} AND n.tariff_code=c.tariff_code " +
                                  $"AND n.calc_cycle=? AND tariff_type IN ('GP-3','GP-4') " +
                                  $"AND a.area_code=n.area_code AND a.region=?";
                            cmd.CommandText = sql;
                            if (netType != "2") cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            break;

                        default: // EntireCEB
                            sql = $"SELECT 'GV1', COUNT(n.acct_number), COALESCE(SUM(n.units_out),0) " +
                                  $"FROM netmtcons n, tariff_code c " +
                                  $"WHERE {netCond} AND n.tariff_code=c.tariff_code " +
                                  $"AND n.calc_cycle=? AND tariff_type IN ('GP-3','GP-4')";
                            cmd.CommandText = sql;
                            if (netType != "2") cmd.Parameters.AddWithValue("?", netType);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            break;
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                        if (reader.Read())
                        {
                            data.Customers = reader[1] == DBNull.Value ? 0 : Convert.ToInt32(reader[1]);
                            data.Units = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdGV1ByNetType netType={netType}");
            }
            return data;
        }

        // ================================================================
        //  BULK — Get All Tariffs
        // ================================================================

        /// <summary>
        /// Gets all distinct bulk tariff codes from the database for the given bill cycle.
        /// Returns actual tariff codes like DM2, GP2, I2, I3, H2, etc.
        /// SQL directly from PDF: "Select tariff from netmtcons where bill_cycle = ? and net_type in ('1','2','3','4') group by tariff order by tariff asc"
        /// </summary>
        private List<string> GetAllBulkTariffs(string billCycle)
        {
            var tariffs = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    // Exact SQL from PDF - no table alias
                    const string sql =
                        "SELECT tariff FROM netmtcons " +
                        "WHERE bill_cycle=? AND net_type IN ('1','2','3','4') " +
                        "GROUP BY tariff ORDER BY tariff ASC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", billCycle);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var tariff = reader[0]?.ToString().Trim();
                                if (!string.IsNullOrEmpty(tariff))
                                {
                                    tariffs.Add(tariff);
                                }
                            }
                        }
                    }
                }
                logger.Info($"GetAllBulkTariffs: Found {tariffs.Count} tariffs - {string.Join(", ", tariffs)}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetAllBulkTariffs EXCEPTION");
            }
            return tariffs;
        }

        // ================================================================
        //  BULK — By Net Type
        // ================================================================

        private NetTypeData GetBulkByNetType(SolarReportType rt, string typeCode,
            string billCycle, List<string> tariffs, string netType)
        {
            var data = new NetTypeData();
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string inClause = string.Join(",", tariffs.ConvertAll(t => "?"));
                    string sql;
                    OleDbCommand cmd = new OleDbCommand { Connection = conn };

                    // Net Metering (net_type='1') doesn't need netmeter join
                    if (netType == "1")
                    {
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = $"SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0) " +
                                      $"FROM netmtcons n, areas a " +
                                      $"WHERE bill_cycle=? AND n.net_type=? " +
                                      $"AND a.area_code=n.area_cd AND a.prov_code=? " +
                                      $"AND n.tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", netType);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;

                            case SolarReportType.Region:
                                sql = $"SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0) " +
                                      $"FROM netmtcons n, areas a " +
                                      $"WHERE bill_cycle=? AND n.net_type=? " +
                                      $"AND a.area_code=n.area_cd AND a.region=? " +
                                      $"AND n.tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", netType);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;

                            default: // EntireCEB
                                sql = $"SELECT COUNT(acc_nbr), COALESCE(SUM(exp_kwd_units),0) " +
                                      $"FROM netmtcons " +
                                      $"WHERE bill_cycle=? AND net_type=? " +
                                      $"AND tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", netType);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;
                        }
                    }
                    else if (netType == "2" || netType == "3")
                    {
                        // Net Accounting, Net Plus, Net Plus Plus need netmeter join and rate filter
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = $"SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0) " +
                                      $"FROM netmtcons n, areas a, netmeter m " +
                                      $"WHERE n.net_type=? AND bill_cycle=? " +
                                      $"AND m.acc_nbr=n.acc_nbr AND rate NOT IN ('0') " +
                                      $"AND a.area_code=n.area_cd AND a.prov_code=? " +
                                      $"AND n.tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", netType);
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;

                            case SolarReportType.Region:
                                sql = $"SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0) " +
                                      $"FROM netmtcons n, areas a, netmeter m " +
                                      $"WHERE n.net_type=? AND bill_cycle=? " +
                                      $"AND m.acc_nbr=n.acc_nbr AND rate NOT IN ('0') " +
                                      $"AND a.area_code=n.area_cd AND a.region=? " +
                                      $"AND n.tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", netType);
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;

                            default: // EntireCEB
                                sql = $"SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0) " +
                                      $"FROM netmtcons n, netmeter m " +
                                      $"WHERE n.net_type=? AND bill_cycle=? " +
                                      $"AND m.acc_nbr=n.acc_nbr AND rate NOT IN ('0') " +
                                      $"AND n.tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", netType);
                                cmd.Parameters.AddWithValue("?", billCycle);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;
                        }
                    }
                    else
                    {
                        // Net Plus Plus (4) needs netmeter join but NO rate filter
                        switch (rt)
                        {
                            case SolarReportType.Province:
                                sql = $"SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0) " +
                                      $"FROM netmtcons n, areas a, netmeter m " +
                                      $"WHERE n.net_type=? AND bill_cycle=? " +
                                      $"AND m.acc_nbr=n.acc_nbr " +
                                      $"AND a.area_code=n.area_cd AND a.prov_code=? " +
                                      $"AND n.tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", netType);
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;

                            case SolarReportType.Region:
                                sql = $"SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0) " +
                                      $"FROM netmtcons n, areas a, netmeter m " +
                                      $"WHERE n.net_type=? AND bill_cycle=? " +
                                      $"AND m.acc_nbr=n.acc_nbr " +
                                      $"AND a.area_code=n.area_cd AND a.region=? " +
                                      $"AND n.tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", netType);
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", typeCode);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;

                            default: // EntireCEB
                                sql = $"SELECT COUNT(n.acc_nbr), COALESCE(SUM(n.exp_kwd_units),0) " +
                                      $"FROM netmtcons n, netmeter m " +
                                      $"WHERE n.net_type=? AND bill_cycle=? " +
                                      $"AND m.acc_nbr=n.acc_nbr " +
                                      $"AND n.tariff IN ({inClause})";
                                cmd.CommandText = sql;
                                cmd.Parameters.AddWithValue("?", netType);
                                cmd.Parameters.AddWithValue("?", billCycle);
                                foreach (var t in tariffs) cmd.Parameters.AddWithValue("?", t);
                                break;
                        }
                    }

                    using (cmd)
                    using (var reader = cmd.ExecuteReader())
                        if (reader.Read())
                        {
                            data.Customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            data.Units = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                        }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkByNetType netType={netType}");
            }
            return data;
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

        // ================================================================
        //  HELPER CLASSES
        // ================================================================

        private class NetTypeData
        {
            public int Customers { get; set; }
            public decimal Units { get; set; }
        }
    }
}