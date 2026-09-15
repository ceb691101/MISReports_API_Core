using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.PUCSLReports;
using MISReports_Api.Models.PUCSLReports.PUCSLSolarConnection;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.PUCSLReports.PUCSLSolarConnection
{
    public class SolarDataUNTCalculationDao
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
        public SolarDataUNTCalculationResponse GetSolarDataUNTReport(PUCSLRequest request)
        {
            var response = new SolarDataUNTCalculationResponse
            {
                Data = new List<SolarDataUNTCalculationModel>()
            };

            if (request.SolarType != SolarNetType.NetMetering && request.SolarType != SolarNetType.NetAccounting && request.SolarType != SolarNetType.NetPlus && request.SolarType != SolarNetType.NetPlusPlus)
            {
                logger.Warn($"SolarType {request.SolarType} is not supported for Solar Data UNT Calculation report.");
                response.ErrorMessage = $"{request.SolarType} is not supported for Solar Data UNT Calculation report. Please select Net Metering, Net Accounting, Net Plus or Net Plus Plus.";
                return response;
            }

            try
            {
                logger.Info("=== START GetSolarDataUNTReport ===");
                logger.Info($"Category={request.ReportCategory}, TypeCode={request.TypeCode}, BillCycle={request.BillCycle}, SolarType={request.SolarType}");

                SolarReportType reportType = MapReportType(request.ReportCategory);

                // Get Year and Month from BillCycle
                var (year, month) = GetYearMonthFromCycle(request.BillCycle);

                // Get all tariff categories in order (from tariff_category table)
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
                    if (request.SolarType == SolarNetType.NetPlusPlus && category != "N++")
                    {
                        continue;
                    }

                    var rowModel = new SolarDataUNTCalculationModel
                    {
                        Category = category,
                        Year = year,
                        Month = month,
                        Accts = 0,
                        UnitsExpD = 0,
                        UnitsExpP = 0,
                        UnitsExpOffP = 0,
                        UnitsImpD = 0,
                        UnitsImpP = 0,
                        UnitsImpOffP = 0
                    };

                    int totAccts = 0;
                    decimal unitsExpDay = 0;
                    decimal unitsImpDay = 0;
                    decimal unitsImpPeakBulk = 0;
                    decimal unitsImpOffPeakBulk = 0;

                    int totAcctsBulk = 0;
                    decimal unitsExpDayBulk = 0;
                    decimal unitsImpDayBulk = 0;

                    decimal kWhpurchased = 0;
                    decimal pdAmt = 0;
                    decimal kWhpurchasedBulk = 0;
                    decimal pdAmtBulk = 0;

                    if (request.SolarType == SolarNetType.NetMetering)
                    {
                        // 1) Get Ordinary tariff codes for this category
                        var ordinaryTariffs = GetTariffCodesForCategory(category, "O");
                        foreach (var tariffCode in ordinaryTariffs)
                        {
                            var ordData = GetOrdinaryData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                            totAccts += ordData.customers;
                            unitsExpDay += ordData.unitsExp;
                            unitsImpDay += ordData.unitsImp;
                        }

                        // 2) Get Bulk tariff codes for this category
                        var bulkTariffs = GetTariffCodesForCategory(category, "B");
                        foreach (var tariffCode in bulkTariffs)
                        {
                            var bulkData = GetBulkData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                            totAcctsBulk += bulkData.customers;
                            unitsExpDayBulk += bulkData.unitsExp;
                            unitsImpDayBulk += bulkData.unitsImp;
                            unitsImpPeakBulk += bulkData.unitsImpPeak;
                            unitsImpOffPeakBulk += bulkData.unitsImpOffPeak;
                        }

                        // 3) Aggregate values
                        rowModel.Accts = totAccts + totAcctsBulk;
                        rowModel.UnitsExpD = unitsExpDay + unitsExpDayBulk;
                        rowModel.UnitsExpP = 0;
                        rowModel.UnitsExpOffP = 0;
                        rowModel.UnitsImpD = unitsImpDay + unitsImpDayBulk;
                        rowModel.UnitsImpP = unitsImpPeakBulk;
                        rowModel.UnitsImpOffP = unitsImpOffPeakBulk;
                    }
                    else if (request.SolarType == SolarNetType.NetAccounting)
                    {
                        // 1) Get Ordinary tariff codes for this category
                        var ordinaryTariffs = GetTariffCodesForCategory(category, "O");
                        foreach (var tariffCode in ordinaryTariffs)
                        {
                            var ordData = GetOrdinaryNetAccountingData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                            totAccts += ordData.customers;
                            kWhpurchased += ordData.kWhpurchased;
                            unitsExpDay += ordData.unitsExp;
                            unitsImpDay += ordData.unitsImp;
                            pdAmt += ordData.pdAmt;
                        }

                        // 2) Get Bulk tariff codes for this category
                        var bulkTariffs = GetTariffCodesForCategory(category, "B");
                        foreach (var tariffCode in bulkTariffs)
                        {
                            var bulkData = GetBulkNetAccountingData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                            totAcctsBulk += bulkData.customers;
                            kWhpurchasedBulk += bulkData.kWhpurchased;
                            unitsExpDayBulk += bulkData.unitsExp;
                            unitsImpDayBulk += bulkData.unitsImp;
                            pdAmtBulk += bulkData.pdAmt;
                        }

                        // 3) Aggregate values
                        rowModel.Accts = totAccts + totAcctsBulk;
                        rowModel.UnitsExpD = unitsExpDay + unitsExpDayBulk;
                        rowModel.UnitsExpP = kWhpurchased + kWhpurchasedBulk;
                        rowModel.UnitsExpOffP = pdAmt + pdAmtBulk;
                        rowModel.UnitsImpD = unitsImpDay + unitsImpDayBulk;
                        rowModel.UnitsImpP = 0;
                        rowModel.UnitsImpOffP = 0;
                    }
                    else if (request.SolarType == SolarNetType.NetPlus)
                    {
                        decimal solarImpBulk = 0;
                        decimal impUnits = 0;
                        decimal impUnitsBulk = 0;

                        // 1) Get Ordinary tariff codes for this category
                        var ordinaryTariffs = GetTariffCodesForCategory(category, "O");
                        foreach (var tariffCode in ordinaryTariffs)
                        {
                            var ordData = GetOrdinaryNetPlusData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                            totAccts += ordData.customers;
                            impUnits += ordData.impUnits;
                            kWhpurchased += ordData.kWhpurchased;
                            pdAmt += ordData.pdAmt;
                        }

                        // 2) Get Bulk tariff codes for this category
                        var bulkTariffs = GetTariffCodesForCategory(category, "B");
                        foreach (var tariffCode in bulkTariffs)
                        {
                            var bulkData = GetBulkNetPlusData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                            totAcctsBulk += bulkData.customers;
                            impUnitsBulk += bulkData.impUnits;
                            kWhpurchasedBulk += bulkData.kWhpurchased;
                            pdAmtBulk += bulkData.pdAmt;
                            solarImpBulk += bulkData.solarImp;
                        }

                        // 3) Aggregate values
                        rowModel.Accts = totAccts + totAcctsBulk;
                        rowModel.UnitsExpP = kWhpurchased + kWhpurchasedBulk;
                        rowModel.UnitsExpOffP = pdAmt + pdAmtBulk;
                        rowModel.UnitsExpD = solarImpBulk; // kWhimp + solarImpBulk where kWhimp = 0
                        rowModel.UnitsImpD = impUnits + impUnitsBulk;
                        rowModel.UnitsImpP = 0;
                        rowModel.UnitsImpOffP = 0;
                    }
                    else if (request.SolarType == SolarNetType.NetPlusPlus)
                    {
                        decimal kWhimp = 0;
                        decimal kWhimpBulk = 0;

                        // 1) Get Ordinary tariff codes for this category (with flag=1)
                        var ordinaryTariffs = GetTariffCodesForCategory(category, "O", true);
                        foreach (var tariffCode in ordinaryTariffs)
                        {
                            var ordData = GetOrdinaryNetPlusPlusData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                            totAccts += ordData.customers;
                            kWhpurchased += ordData.kWhpurchased;
                            pdAmt += ordData.pdAmt;
                            kWhimp += ordData.kWhimp;
                        }

                        // 2) Get Bulk tariff codes for this category (with flag=1)
                        var bulkTariffs = GetTariffCodesForCategory(category, "B", true);
                        foreach (var tariffCode in bulkTariffs)
                        {
                            var bulkData = GetBulkNetPlusPlusData(reportType, request.TypeCode, request.BillCycle, tariffCode);
                            totAcctsBulk += bulkData.customers;
                            kWhpurchasedBulk += bulkData.kWhpurchased;
                            pdAmtBulk += bulkData.pdAmt;
                            kWhimpBulk += bulkData.kWhimp;
                        }

                        // 3) Aggregate values
                        rowModel.Accts = totAccts + totAcctsBulk;
                        rowModel.UnitsExpP = kWhpurchased + kWhpurchasedBulk; // Solar meter_export_kWh
                        rowModel.UnitsExpOffP = pdAmt + pdAmtBulk; // paid_amount
                        rowModel.UnitsExpD = kWhimp + kWhimpBulk; // Solar meter_import_kWh
                        rowModel.UnitsImpD = 0; // Not used / hidden
                        rowModel.UnitsImpP = 0;
                        rowModel.UnitsImpOffP = 0;
                    }

                    response.Data.Add(rowModel);
                }

                // 4) Calculate Total
                var totalRow = new SolarDataUNTCalculationModel
                {
                    Category = "Total",
                    Year = year,
                    Month = month,
                    Accts = 0,
                    UnitsExpD = 0,
                    UnitsExpP = 0,
                    UnitsExpOffP = 0,
                    UnitsImpD = 0,
                    UnitsImpP = 0,
                    UnitsImpOffP = 0
                };

                foreach (var row in response.Data)
                {
                    totalRow.Accts += row.Accts;
                    totalRow.UnitsExpD += row.UnitsExpD;
                    totalRow.UnitsExpP += row.UnitsExpP;
                    totalRow.UnitsExpOffP += row.UnitsExpOffP;
                    totalRow.UnitsImpD += row.UnitsImpD;
                    totalRow.UnitsImpP += row.UnitsImpP;
                    totalRow.UnitsImpOffP += row.UnitsImpOffP;
                }

                response.Total = totalRow;
                response.ErrorMessage = string.Empty;
                logger.Info($"SolarDataUNTReport completed. {response.Data.Count} categories.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetSolarDataUNTReport EXCEPTION");
                response.ErrorMessage = $"Error: {ex.Message}";
            }

            return response;
        }

        // ================================================================
        //  GET TARIFF CATEGORIES
        // ================================================================
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
        private List<string> GetTariffCodesForCategory(string category, string cusCategory, bool isNetPlusPlus = false)
        {
            var tariffCodes = new List<string>();
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string sql;
                    if (isNetPlusPlus)
                    {
                        sql = "SELECT c.tariff_code FROM cat_tariff_table c " +
                              "WHERE c.cus_cat=? AND c.flag=1";
                    }
                    else
                    {
                        sql = "SELECT c.tariff_code FROM cat_tariff_table c, tariff_category t " +
                              "WHERE c.tariff_cat=t.tariff_cat AND t.tariff_cat=? AND c.cus_cat=?";
                    }

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (isNetPlusPlus)
                        {
                            cmd.Parameters.AddWithValue("?", cusCategory);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", category);
                            cmd.Parameters.AddWithValue("?", cusCategory);
                        }

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
        //  GET ORDINARY DATA NET METERING
        // ================================================================
        private (int customers, decimal unitsExp, decimal unitsImp) GetOrdinaryData(SolarReportType rt, string typeCode,
            string calcCycle, string tariffCode)
        {
            int customers = 0;
            decimal unitsExp = 0;
            decimal unitsImp = 0;

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string countSql = "";
                    string unitsSql = "";

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            countSql = "SELECT COUNT(n.acct_number) FROM netmtcons n, areas a " +
                                       "WHERE n.net_type='1' AND n.calc_cycle=? " +
                                       "AND a.area_code=n.area_code AND a.prov_code=? AND n.tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(units_out),0), COALESCE(SUM(units_in),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE tariff_code=? AND n.calc_cycle=? AND n.net_type='1' " +
                                       "AND a.area_code=n.area_code AND a.prov_code=?";
                            break;

                        case SolarReportType.Region:
                            countSql = "SELECT COUNT(n.acct_number) FROM netmtcons n, areas a " +
                                       "WHERE n.net_type='1' AND n.calc_cycle=? " +
                                       "AND a.area_code=n.area_code AND a.region=? AND n.tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(units_out),0), COALESCE(SUM(units_in),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE tariff_code=? AND n.calc_cycle=? AND n.net_type='1' " +
                                       "AND a.area_code=n.area_code AND a.region=?";
                            break;

                        default: // EntireCEB
                            countSql = "SELECT COUNT(acct_number) FROM netmtcons " +
                                       "WHERE net_type='1' AND calc_cycle=? AND tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(units_out),0), COALESCE(SUM(units_in),0) " +
                                       "FROM netmtcons " +
                                       "WHERE tariff_code=? AND calc_cycle=? AND net_type='1'";
                            break;
                    }

                    // 1) Execute Count Query
                    using (var cmd = new OleDbCommand(countSql, conn))
                    {
                        if (rt == SolarReportType.Province || rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            }
                        }
                    }

                    // 2) Execute Units Query
                    using (var cmd = new OleDbCommand(unitsSql, conn))
                    {
                        if (rt == SolarReportType.Province || rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                unitsExp = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]); // units_out
                                unitsImp = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]); // units_in
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdinaryData tariffCode={tariffCode}");
            }

            return (customers, unitsExp, unitsImp);
        }

        // ================================================================
        //  GET BULK DATA NET METERING
        // ================================================================
        private (int customers, decimal unitsExp, decimal unitsImp, decimal unitsImpPeak, decimal unitsImpOffPeak) GetBulkData(
            SolarReportType rt, string typeCode, string billCycle, string tariff)
        {
            int customers = 0;
            decimal unitsExp = 0;
            decimal unitsImp = 0;
            decimal unitsImpPeak = 0;
            decimal unitsImpOffPeak = 0;

            try
            {
                string bulkTypeCode = (rt == SolarReportType.Province)
                    ? typeCode.PadLeft(2, '0')  // "3" → "03"
                    : typeCode;

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    string countSql = "";
                    string unitsSql = "";

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            countSql = "SELECT COUNT(n.acc_nbr) FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND n.net_type='1' AND a.area_code=n.area_cd AND a.prov_code=? AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(n.exp_kwd_units),0), COALESCE(SUM(n.imp_kwd_units),0), " +
                                       "COALESCE(SUM(n.imp_kwp_units),0), COALESCE(SUM(n.imp_kwo_units),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND tariff=? AND n.net_type='1' AND a.area_code=n.area_cd AND a.prov_code=?";
                            break;

                        case SolarReportType.Region:
                            countSql = "SELECT COUNT(n.acc_nbr) FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND n.net_type='1' AND a.area_code=n.area_cd AND a.region=? AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(n.exp_kwd_units),0), COALESCE(SUM(n.imp_kwd_units),0), " +
                                       "COALESCE(SUM(n.imp_kwp_units),0), COALESCE(SUM(n.imp_kwo_units),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND tariff=? AND n.net_type='1' AND a.area_code=n.area_cd AND a.region=?";
                            break;

                        default: // EntireCEB
                            countSql = "SELECT COUNT(acc_nbr) FROM netmtcons " +
                                       "WHERE bill_cycle=? AND net_type='1' AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(exp_kwd_units),0), COALESCE(SUM(imp_kwd_units),0), " +
                                       "COALESCE(SUM(imp_kwp_units),0), COALESCE(SUM(imp_kwo_units),0) " +
                                       "FROM netmtcons " +
                                       "WHERE bill_cycle=? AND tariff=? AND net_type='1'";
                            break;
                    }

                    // 1) Execute Count Query
                    using (var cmd = new OleDbCommand(countSql, conn))
                    {
                        if (rt == SolarReportType.Province)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }
                        else if (rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            }
                        }
                    }

                    // 2) Execute Units Query
                    using (var cmd = new OleDbCommand(unitsSql, conn))
                    {
                        if (rt == SolarReportType.Province)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                        }
                        else if (rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            cmd.Parameters.AddWithValue("?", typeCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                unitsExp = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]);        // exp_kwd_units
                                unitsImp = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);        // imp_kwd_units
                                unitsImpPeak = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);    // imp_kwp_units
                                unitsImpOffPeak = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]); // imp_kwo_units
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkData tariff={tariff}");
            }

            return (customers, unitsExp, unitsImp, unitsImpPeak, unitsImpOffPeak);
        }

        // ================================================================
        //  GET ORDINARY NET ACCOUNTING DATA
        // ================================================================
        private (int customers, decimal kWhpurchased, decimal unitsExp, decimal unitsImp, decimal pdAmt) GetOrdinaryNetAccountingData(
            SolarReportType rt, string typeCode, string calcCycle, string tariffCode)
        {
            int customers = 0;
            decimal kWhpurchased = 0;
            decimal unitsExp = 0;
            decimal unitsImp = 0;
            decimal pdAmt = 0;

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string countSql = "";
                    string unitsSql = "";

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            countSql = "SELECT COUNT(n.acct_number) FROM netmtcons n, areas a " +
                                       "WHERE (n.net_type='2' OR n.net_type='5') AND n.calc_cycle=? " +
                                       "AND a.area_code=n.area_code AND a.prov_code=? AND n.tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(units_out),0), COALESCE(SUM(units_in),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE tariff_code=? AND n.calc_cycle=? AND (n.net_type='2' OR n.net_type='5') " +
                                       "AND a.area_code=n.area_code AND a.prov_code=?";
                            break;

                        case SolarReportType.Region:
                            countSql = "SELECT COUNT(n.acct_number) FROM netmtcons n, areas a " +
                                       "WHERE (n.net_type='2' OR n.net_type='5') AND n.calc_cycle=? " +
                                       "AND a.area_code=n.area_code AND a.region=? AND n.tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(units_out),0), COALESCE(SUM(units_in),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE tariff_code=? AND n.calc_cycle=? AND (n.net_type='2' OR n.net_type='5') " +
                                       "AND a.area_code=n.area_code AND a.region=?";
                            break;

                        default: // EntireCEB
                            countSql = "SELECT COUNT(acct_number) FROM netmtcons " +
                                       "WHERE (net_type='2' OR net_type='5') AND calc_cycle=? AND tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(units_out),0), COALESCE(SUM(units_in),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons " +
                                       "WHERE tariff_code=? AND calc_cycle=? AND (net_type='2' OR net_type='5')";
                            break;
                    }

                    // 1) Execute Count Query
                    using (var cmd = new OleDbCommand(countSql, conn))
                    {
                        if (rt == SolarReportType.Province || rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            }
                        }
                    }

                    // 2) Execute Units Query
                    using (var cmd = new OleDbCommand(unitsSql, conn))
                    {
                        if (rt == SolarReportType.Province || rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                kWhpurchased = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]);
                                unitsExp = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                                unitsImp = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                                pdAmt = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdinaryNetAccountingData tariffCode={tariffCode}");
            }

            return (customers, kWhpurchased, unitsExp, unitsImp, pdAmt);
        }

        // ================================================================
        //  GET BULK NET ACCOUNTING DATA
        // ================================================================
        private (int customers, decimal kWhpurchased, decimal unitsExp, decimal unitsImp, decimal pdAmt) GetBulkNetAccountingData(
            SolarReportType rt, string typeCode, string billCycle, string tariff)
        {
            int customers = 0;
            decimal kWhpurchased = 0;
            decimal unitsExp = 0;
            decimal unitsImp = 0;
            decimal pdAmt = 0;

            try
            {
                string bulkTypeCode = (rt == SolarReportType.Province)
                    ? typeCode.PadLeft(2, '0')  // "3" → "03"
                    : typeCode;

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    string countSql = "";
                    string unitsSql = "";

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            countSql = "SELECT COUNT(n.acc_nbr) FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND n.net_type='2' AND a.area_code=n.area_cd AND a.prov_code=? AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(exp_kwd_units),0), COALESCE(SUM(imp_kwo_units+imp_kwd_units+imp_kwp_units),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND tariff=? AND n.net_type='2' AND a.area_code=n.area_cd AND a.prov_code=?";
                            break;

                        case SolarReportType.Region:
                            countSql = "SELECT COUNT(n.acc_nbr) FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND n.net_type='2' AND a.area_code=n.area_cd AND a.region=? AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(exp_kwd_units),0), COALESCE(SUM(imp_kwo_units+imp_kwd_units+imp_kwp_units),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND tariff=? AND n.net_type='2' AND a.area_code=n.area_cd AND a.region=?";
                            break;

                        default: // EntireCEB
                            countSql = "SELECT COUNT(acc_nbr) FROM netmtcons " +
                                       "WHERE bill_cycle=? AND net_type='2' AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(exp_kwd_units),0), COALESCE(SUM(imp_kwo_units+imp_kwd_units+imp_kwp_units),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons " +
                                       "WHERE bill_cycle=? AND tariff=? AND net_type='2'";
                            break;
                    }

                    // 1) Execute Count Query
                    using (var cmd = new OleDbCommand(countSql, conn))
                    {
                        if (rt == SolarReportType.Province)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }
                        else if (rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            }
                        }
                    }

                    // 2) Execute Units Query
                    using (var cmd = new OleDbCommand(unitsSql, conn))
                    {
                        if (rt == SolarReportType.Province)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                        }
                        else if (rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            cmd.Parameters.AddWithValue("?", typeCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                kWhpurchased = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]);
                                unitsExp = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                                unitsImp = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                                pdAmt = reader[3] == DBNull.Value ? 0 : Convert.ToDecimal(reader[3]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkNetAccountingData tariff={tariff}");
            }

            return (customers, kWhpurchased, unitsExp, unitsImp, pdAmt);
        }

        // ================================================================
        //  GET ORDINARY NET PLUS DATA
        // ================================================================
        private (int customers, decimal impUnits, decimal kWhpurchased, decimal pdAmt) GetOrdinaryNetPlusData(
            SolarReportType rt, string typeCode, string calcCycle, string tariffCode)
        {
            int customers = 0;
            decimal impUnits = 0;
            decimal kWhpurchased = 0;
            decimal pdAmt = 0;

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string countSql = "";
                    string unitsSql = "";

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            countSql = "SELECT COUNT(n.acct_number), SUM(n.units_in) FROM netmtcons n, areas a " +
                                       "WHERE n.net_type='3' AND n.calc_cycle=? " +
                                       "AND a.area_code=n.area_code AND a.prov_code=? AND n.tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE tariff_code=? AND n.calc_cycle=? AND n.net_type='3' " +
                                       "AND a.area_code=n.area_code AND a.prov_code=?";
                            break;

                        case SolarReportType.Region:
                            countSql = "SELECT COUNT(n.acct_number), SUM(n.units_in) FROM netmtcons n, areas a " +
                                       "WHERE n.net_type='3' AND n.calc_cycle=? " +
                                       "AND a.area_code=n.area_code AND a.region=? AND n.tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE tariff_code=? AND n.calc_cycle=? AND n.net_type='3' " +
                                       "AND a.area_code=n.area_code AND a.region=?";
                            break;

                        default: // EntireCEB
                            countSql = "SELECT COUNT(acct_number), SUM(units_in) FROM netmtcons " +
                                       "WHERE net_type='3' AND calc_cycle=? AND tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0) " +
                                       "FROM netmtcons " +
                                       "WHERE tariff_code=? AND calc_cycle=? AND net_type='3'";
                            break;
                    }

                    // 1) Execute Count/Import Query
                    using (var cmd = new OleDbCommand(countSql, conn))
                    {
                        if (rt == SolarReportType.Province || rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                                impUnits = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                            }
                        }
                    }

                    // 2) Execute Units Query
                    using (var cmd = new OleDbCommand(unitsSql, conn))
                    {
                        if (rt == SolarReportType.Province || rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                kWhpurchased = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]);
                                pdAmt = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdinaryNetPlusData tariffCode={tariffCode}");
            }

            return (customers, impUnits, kWhpurchased, pdAmt);
        }

        // ================================================================
        //  GET BULK NET PLUS DATA
        // ================================================================
        private (int customers, decimal impUnits, decimal kWhpurchased, decimal pdAmt, decimal solarImp) GetBulkNetPlusData(
            SolarReportType rt, string typeCode, string billCycle, string tariff)
        {
            int customers = 0;
            decimal impUnits = 0;
            decimal kWhpurchased = 0;
            decimal pdAmt = 0;
            decimal solarImp = 0;

            try
            {
                string bulkTypeCode = (rt == SolarReportType.Province)
                    ? typeCode.PadLeft(2, '0')  // "3" → "03"
                    : typeCode;

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    string countSql = "";
                    string unitsSql = "";

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            countSql = "SELECT COUNT(n.acc_nbr), SUM(n.imp_kwo_units+n.imp_kwp_units+n.imp_kwd_units) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND n.net_type='3' AND a.area_code=n.area_cd AND a.prov_code=? AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(exp_imp_kwd_units),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND tariff=? AND n.net_type='3' AND a.area_code=n.area_cd AND a.prov_code=?";
                            break;

                        case SolarReportType.Region:
                            countSql = "SELECT COUNT(n.acc_nbr), SUM(n.imp_kwo_units+n.imp_kwp_units+n.imp_kwd_units) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND n.net_type='3' AND a.area_code=n.area_cd AND a.region=? AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(exp_imp_kwd_units),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND tariff=? AND n.net_type='3' AND a.area_code=n.area_cd AND a.region=?";
                            break;

                        default: // EntireCEB
                            countSql = "SELECT COUNT(acc_nbr), SUM(imp_kwo_units+imp_kwp_units+imp_kwd_units) FROM netmtcons " +
                                       "WHERE bill_cycle=? AND net_type='3' AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(exp_imp_kwd_units),0) " +
                                       "FROM netmtcons " +
                                       "WHERE bill_cycle=? AND tariff=? AND net_type='3'";
                            break;
                    }

                    // 1) Execute Count/Import Query
                    using (var cmd = new OleDbCommand(countSql, conn))
                    {
                        if (rt == SolarReportType.Province)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }
                        else if (rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                                impUnits = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                            }
                        }
                    }

                    // 2) Execute Units Query
                    using (var cmd = new OleDbCommand(unitsSql, conn))
                    {
                        if (rt == SolarReportType.Province)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                        }
                        else if (rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            cmd.Parameters.AddWithValue("?", typeCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                kWhpurchased = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]);
                                pdAmt = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                                solarImp = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkNetPlusData tariff={tariff}");
            }

            return (customers, impUnits, kWhpurchased, pdAmt, solarImp);
        }

        // ================================================================
        //  GET ORDINARY NET PLUS PLUS DATA
        // ================================================================
        private (int customers, decimal kWhpurchased, decimal pdAmt, decimal kWhimp) GetOrdinaryNetPlusPlusData(
            SolarReportType rt, string typeCode, string calcCycle, string tariffCode)
        {
            int customers = 0;
            decimal kWhpurchased = 0;
            decimal pdAmt = 0;
            decimal kWhimp = 0;

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    string countSql = "";
                    string unitsSql = "";

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            countSql = "SELECT COUNT(n.acct_number) FROM netmtcons n, areas a " +
                                       "WHERE n.net_type='4' AND n.calc_cycle=? " +
                                       "AND a.area_code=n.area_code AND a.prov_code=? AND n.tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(units_in),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE tariff_code=? AND n.calc_cycle=? AND n.net_type='4' " +
                                       "AND a.area_code=n.area_code AND a.prov_code=?";
                            break;

                        case SolarReportType.Region:
                            countSql = "SELECT COUNT(n.acct_number) FROM netmtcons n, areas a " +
                                       "WHERE n.net_type='4' AND n.calc_cycle=? " +
                                       "AND a.area_code=n.area_code AND a.region=? AND n.tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(units_in),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE tariff_code=? AND n.calc_cycle=? AND n.net_type='4' " +
                                       "AND a.area_code=n.area_code AND a.region=?";
                            break;

                        default: // EntireCEB
                            countSql = "SELECT COUNT(acct_number) FROM netmtcons " +
                                       "WHERE net_type='4' AND calc_cycle=? AND tariff_code=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(units_in),0) " +
                                       "FROM netmtcons " +
                                       "WHERE tariff_code=? AND calc_cycle=? AND net_type='4'";
                            break;
                    }

                    // 1) Execute Count Query
                    using (var cmd = new OleDbCommand(countSql, conn))
                    {
                        if (rt == SolarReportType.Province || rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", tariffCode);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            }
                        }
                    }

                    // 2) Execute Units Query
                    using (var cmd = new OleDbCommand(unitsSql, conn))
                    {
                        if (rt == SolarReportType.Province || rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", tariffCode);
                            cmd.Parameters.AddWithValue("?", calcCycle);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                kWhpurchased = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]);
                                pdAmt = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                                kWhimp = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetOrdinaryNetPlusPlusData tariffCode={tariffCode}");
            }

            return (customers, kWhpurchased, pdAmt, kWhimp);
        }

        // ================================================================
        //  GET BULK NET PLUS PLUS DATA
        // ================================================================
        private (int customers, decimal kWhpurchased, decimal pdAmt, decimal kWhimp) GetBulkNetPlusPlusData(
            SolarReportType rt, string typeCode, string billCycle, string tariff)
        {
            int customers = 0;
            decimal kWhpurchased = 0;
            decimal pdAmt = 0;
            decimal kWhimp = 0;

            try
            {
                string bulkTypeCode = (rt == SolarReportType.Province)
                    ? typeCode.PadLeft(2, '0')  // "3" → "03"
                    : typeCode;

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    string countSql = "";
                    string unitsSql = "";

                    switch (rt)
                    {
                        case SolarReportType.Province:
                            countSql = "SELECT COUNT(n.acc_nbr) FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND n.net_type='4' AND a.area_code=n.area_cd AND a.prov_code=? AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(imp_kwo_units+imp_kwd_units+imp_kwp_units),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND tariff=? AND n.net_type='4' AND a.area_code=n.area_cd AND a.prov_code=?";
                            break;

                        case SolarReportType.Region:
                            countSql = "SELECT COUNT(n.acc_nbr) FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND n.net_type='4' AND a.area_code=n.area_cd AND a.region=? AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(imp_kwo_units+imp_kwd_units+imp_kwp_units),0) " +
                                       "FROM netmtcons n, areas a " +
                                       "WHERE bill_cycle=? AND tariff=? AND n.net_type='4' AND a.area_code=n.area_cd AND a.region=?";
                            break;

                        default: // EntireCEB
                            countSql = "SELECT COUNT(acc_nbr) FROM netmtcons " +
                                       "WHERE bill_cycle=? AND net_type='4' AND tariff=?";

                            unitsSql = "SELECT COALESCE(SUM(unitsale),0), COALESCE(SUM(kwh_sales),0), COALESCE(SUM(imp_kwo_units+imp_kwd_units+imp_kwp_units),0) " +
                                       "FROM netmtcons " +
                                       "WHERE bill_cycle=? AND tariff=? AND net_type='4'";
                            break;
                    }

                    // 1) Execute Count Query
                    using (var cmd = new OleDbCommand(countSql, conn))
                    {
                        if (rt == SolarReportType.Province)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }
                        else if (rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", typeCode);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customers = reader[0] == DBNull.Value ? 0 : Convert.ToInt32(reader[0]);
                            }
                        }
                    }

                    // 2) Execute Units Query
                    using (var cmd = new OleDbCommand(unitsSql, conn))
                    {
                        if (rt == SolarReportType.Province)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            cmd.Parameters.AddWithValue("?", bulkTypeCode);
                        }
                        else if (rt == SolarReportType.Region)
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                            cmd.Parameters.AddWithValue("?", typeCode);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("?", billCycle);
                            cmd.Parameters.AddWithValue("?", tariff);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                kWhpurchased = reader[0] == DBNull.Value ? 0 : Convert.ToDecimal(reader[0]);
                                pdAmt = reader[1] == DBNull.Value ? 0 : Convert.ToDecimal(reader[1]);
                                kWhimp = reader[2] == DBNull.Value ? 0 : Convert.ToDecimal(reader[2]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"GetBulkNetPlusPlusData tariff={tariff}");
            }

            return (customers, kWhpurchased, pdAmt, kWhimp);
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

        private (string year, string month) GetYearMonthFromCycle(string billCycle)
        {
            try
            {
                int cycle = int.Parse(billCycle);
                string monthYear = BillCycleHelper.ConvertToMonthYear(cycle);

                if (string.IsNullOrEmpty(monthYear) || monthYear == "Invalid" || monthYear == "Unknown")
                {
                    logger.Warn($"Invalid bill cycle: {billCycle}");
                    return ("", "");
                }

                var parts = monthYear.Split(' ');
                if (parts.Length == 2)
                {
                    string monthName = parts[0];
                    string year = parts[1];
                    string monthNumber = BillCycleHelper.ConvertMonthNameToNumber(monthName);
                    return (year, monthNumber);
                }

                return ("", "");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error parsing bill cycle {billCycle}");
                return ("", "");
            }
        }
    }
}
