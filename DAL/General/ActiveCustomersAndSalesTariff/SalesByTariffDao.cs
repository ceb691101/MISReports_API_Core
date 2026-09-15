using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff
{
    // ════════════════════════════════════════════════════════════════════════════
    //  SalesByTariffOrdinaryDao
    //  Queries the ORDINARY Informix database (consmry / tariff_code / areas /
    //  provinces) to sum kWh sales (cons_kwh) grouped by tariff class.
    //  No location filter – all areas / provinces / regions are returned.
    // ════════════════════════════════════════════════════════════════════════════
    public class SalesByTariffOrdinaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, useBulkConnection: false);
        }

        /// <summary>
        /// Returns every (location | calc_cycle | tariff_class | kwh_sales) row
        /// for the chosen report granularity and cycle range.
        /// </summary>
        public List<SalesByTariffOrdinaryModel> GetSalesByTariffOrdinaryReport(SalesByTariffRequest request)
        {
            var results = new List<SalesByTariffOrdinaryModel>();

            try
            {
                logger.Info("=== START GetSalesByTariffOrdinaryReport ===");
                logger.Info($"ReportType={request.ReportType}, FromCycle={request.FromCycle}, ToCycle={request.ToCycle}");

                using (var conn = _dbConnection.GetConnection(useBulkConnection: false))
                {
                    conn.Open();

                    string sql = BuildOrdinarySql(request.ReportType);

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fromCycle", request.FromCycle);
                        cmd.Parameters.AddWithValue("@toCycle", request.ToCycle);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Column layout for every variant:
                                //   col 0 – location key  (area_code | prov_code | a.Region | 'Entire CEB')
                                //   col 1 – calc_cycle
                                //   col 2 – tariff_class
                                //   col 3 – sum(cons_kwh)
                                // Area & Province variants join provinces and carry extra name cols:
                                //   col 4 – area_name or prov_name
                                //   col 5 – prov_name or region
                                var model = new SalesByTariffOrdinaryModel
                                {
                                    BillCycle = GetStringValue(reader, 1),
                                    TariffClass = GetStringValue(reader, 2),
                                    KwhSales = GetDecimalValue(reader, 3),
                                    ErrorMessage = string.Empty
                                };

                                string locationKey = GetStringValue(reader, 0);

                                switch (request.ReportType)
                                {
                                    case SalesByTariffReportType.Area:
                                        // col 4 = area_name, col 5 = prov_name
                                        model.Area = GetStringValue(reader, 4);
                                        model.Province = GetStringValue(reader, 5);
                                        break;

                                    case SalesByTariffReportType.Province:
                                        // col 4 = prov_name, col 5 = region
                                        model.Province = GetStringValue(reader, 4);
                                        model.Division = GetStringValue(reader, 5);
                                        break;

                                    case SalesByTariffReportType.Region:
                                        // col 0 = a.Region (already the display value)
                                        model.Division = locationKey;
                                        break;

                                    case SalesByTariffReportType.EntireCEB:
                                        // col 0 = literal 'Entire CEB'
                                        break;
                                }

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetSalesByTariffOrdinaryReport – {results.Count} rows ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetSalesByTariffOrdinaryReport");
                throw;
            }
        }

        /// <summary>
        /// Builds the SQL for the chosen granularity.
        ///
        /// Area     → groups by c.area_code; extra cols: a.area_name, p.prov_name
        /// Province → groups by a.prov_code; extra cols: p.prov_name, a.region
        /// Region   → groups by a.Region;    no extra name cols needed
        /// EntireCEB→ constant 'Entire CEB'; no extra cols needed
        ///
        /// Aggregated column: sum(c.cons_kwh)  [vs sum(c.cnt) in ActiveCustomers]
        /// Parameters: (fromCycle, toCycle) – no location filter.
        /// </summary>
        private string BuildOrdinarySql(SalesByTariffReportType reportType)
        {
            switch (reportType)
            {
                case SalesByTariffReportType.Area:
                    // Original: Select c.area_code,c.calc_cycle,t.tariff_class,sum(c.cons_kwh)
                    //           from consmry c, tariff_code t, areas a
                    //           where c.tariff_code=t.tariff_code
                    //             and (c.calc_cycle >=? and c.calc_cycle<=?)
                    //             and c.area_code=a.area_code
                    //           group by 1,2,3 order by 1,2,3
                    return @"
                        SELECT c.area_code, c.calc_cycle, t.tariff_class, sum(c.cons_kwh),
                               a.area_name, p.prov_name
                        FROM   consmry c, tariff_code t, areas a, provinces p
                        WHERE  c.tariff_code = t.tariff_code
                          AND  (c.calc_cycle >= ? AND c.calc_cycle <= ?)
                          AND  c.area_code   = a.area_code
                          AND  a.prov_code   = p.prov_code
                        GROUP BY c.area_code, c.calc_cycle, t.tariff_class,
                                 a.area_name, p.prov_name
                        ORDER BY c.area_code, c.calc_cycle, t.tariff_class";

                case SalesByTariffReportType.Province:
                    // Original: Select a.prov_code,c.calc_cycle,t.tariff_class,sum(c.cons_kwh)
                    //           group by 1,2,3 order by 1,2,3
                    return @"
                        SELECT a.prov_code, c.calc_cycle, t.tariff_class, sum(c.cons_kwh),
                               p.prov_name, a.region
                        FROM   consmry c, tariff_code t, areas a, provinces p
                        WHERE  c.tariff_code = t.tariff_code
                          AND  (c.calc_cycle >= ? AND c.calc_cycle <= ?)
                          AND  c.area_code   = a.area_code
                          AND  a.prov_code   = p.prov_code
                        GROUP BY a.prov_code, c.calc_cycle, t.tariff_class,
                                 p.prov_name, a.region
                        ORDER BY a.prov_code, c.calc_cycle, t.tariff_class";

                case SalesByTariffReportType.Region:
                    // Original: Select a.Region,c.calc_cycle,t.tariff_class,sum(c.cons_kwh)
                    //           group by 1,2,3 order by 1,2,3
                    return @"
                        SELECT a.Region, c.calc_cycle, t.tariff_class, sum(c.cons_kwh)
                        FROM   consmry c, tariff_code t, areas a
                        WHERE  c.tariff_code = t.tariff_code
                          AND  (c.calc_cycle >= ? AND c.calc_cycle <= ?)
                          AND  c.area_code   = a.area_code
                        GROUP BY a.Region, c.calc_cycle, t.tariff_class
                        ORDER BY a.Region, c.calc_cycle, t.tariff_class";

                case SalesByTariffReportType.EntireCEB:
                default:
                    // Original: Select 'Entire CEB',c.calc_cycle,t.tariff_class,sum(c.cons_kwh)
                    //           group by 1,2,3 order by 1,2,3
                    return @"
                        SELECT 'Entire CEB', c.calc_cycle, t.tariff_class, sum(c.cons_kwh)
                        FROM   consmry c, tariff_code t, areas a
                        WHERE  c.tariff_code = t.tariff_code
                          AND  (c.calc_cycle >= ? AND c.calc_cycle <= ?)
                          AND  c.area_code   = a.area_code
                        GROUP BY c.calc_cycle, t.tariff_class
                        ORDER BY c.calc_cycle, t.tariff_class";
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private string GetStringValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString()?.Trim();
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read string at ordinal {ordinal}");
                return null;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                if (reader.IsDBNull(ordinal)) return 0m;
                return Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read decimal at ordinal {ordinal}");
                return 0m;
            }
        }
    }


    // ════════════════════════════════════════════════════════════════════════════
    //  SalesByTariffBulkDao
    //  Queries the BULK Informix database (account_info / areas / provinces).
    //  Aggregated column: sum(kwh_units)  [vs sum(no_acc) in ActiveCustomers]
    //  TM1 is always excluded. No location filter.
    // ════════════════════════════════════════════════════════════════════════════
    public class SalesByTariffBulkDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, useBulkConnection: true);
        }

        /// <summary>
        /// Returns every (location | bill_cycle | tariff | kwh_sales) row
        /// for the chosen report granularity and cycle range.
        /// </summary>
        public List<SalesByTariffBulkModel> GetSalesByTariffBulkReport(SalesByTariffRequest request)
        {
            var results = new List<SalesByTariffBulkModel>();

            try
            {
                logger.Info("=== START GetSalesByTariffBulkReport ===");
                logger.Info($"ReportType={request.ReportType}, FromCycle={request.FromCycle}, ToCycle={request.ToCycle}");

                using (var conn = _dbConnection.GetConnection(useBulkConnection: true))
                {
                    conn.Open();

                    string sql = BuildBulkSql(request.ReportType);

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@fromCycle", request.FromCycle);
                        cmd.Parameters.AddWithValue("@toCycle", request.ToCycle);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Bulk column layout (same across all variants):
                                //   col 0 – division / region  ('' for Area / EntireCEB)
                                //   col 1 – province / 'Entire CEB'
                                //   col 2 – area_name or area_cd ('' for non-Area reports)
                                //   col 3 – bill_cycle
                                //   col 4 – tariff
                                //   col 5 – sum(kwh_units)
                                // Area report carries area_cd as an extra col 6
                                var model = new SalesByTariffBulkModel
                                {
                                    Division = GetStringValue(reader, 0),
                                    Province = GetStringValue(reader, 1),
                                    BillCycle = GetStringValue(reader, 3),
                                    Tariff = GetStringValue(reader, 4),
                                    KwhSales = GetDecimalValue(reader, 5),
                                    ErrorMessage = string.Empty
                                };

                                if (request.ReportType == SalesByTariffReportType.Area)
                                {
                                    model.Area = GetStringValue(reader, 2); // AREA_NAME
                                    model.AreaCode = GetStringValue(reader, 6); // area_cd
                                }

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetSalesByTariffBulkReport – {results.Count} rows ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetSalesByTariffBulkReport");
                throw;
            }
        }

        /// <summary>
        /// Builds the bulk SELECT statement for the chosen granularity.
        ///
        /// Column order is always:
        ///   division | province | area_name | bill_cycle | tariff | sum(kwh_units) [| area_cd for Area]
        ///
        /// Aggregated column: sum(kwh_units)  [vs sum(no_acc) in ActiveCustomers]
        /// Parameters: (fromCycle, toCycle) – no location filter.
        /// TM1 excluded in all variants.
        /// </summary>
        private string BuildBulkSql(SalesByTariffReportType reportType)
        {
            switch (reportType)
            {
                case SalesByTariffReportType.Area:
                    // Original: Select p.prov_name,AREA_NAME,area_cd,bill_cycle,tariff,sum(kwh_units)
                    //           from account_info a,areas b, provinces p
                    //           where a.area_cd = b.area_code And (bill_cycle >= ? And bill_cycle <= ?)
                    //             and b.prov_code=p.prov_code and tariff not in ('TM1')
                    //           group by 1,2,3,4,5 order by 1,2,3,4,5
                    return @"
                        SELECT p.prov_name, b.AREA_NAME, a.area_cd, a.bill_cycle, a.tariff,
                               sum(a.kwh_units), a.area_cd
                        FROM   account_info a, areas b, provinces p
                        WHERE  a.area_cd     = b.area_code
                          AND  (a.bill_cycle >= ? AND a.bill_cycle <= ?)
                          AND  b.prov_code   = p.prov_code
                          AND  a.tariff     NOT IN ('TM1')
                        GROUP BY p.prov_name, b.AREA_NAME, a.area_cd, a.bill_cycle, a.tariff
                        ORDER BY p.prov_name, b.AREA_NAME, a.area_cd, a.bill_cycle, a.tariff";

                case SalesByTariffReportType.Province:
                    // Original: Select region,p.prov_name,'',bill_cycle,tariff,sum(kwh_units)
                    //           group by 1,2,3,4,5 order by 1,2,3,4,5
                    return @"
                        SELECT b.region, p.prov_name, '', a.bill_cycle, a.tariff,
                               sum(a.kwh_units)
                        FROM   account_info a, areas b, provinces p
                        WHERE  a.area_cd     = b.area_code
                          AND  (a.bill_cycle >= ? AND a.bill_cycle <= ?)
                          AND  b.prov_code   = p.prov_code
                          AND  a.tariff     NOT IN ('TM1')
                        GROUP BY b.region, p.prov_name, a.bill_cycle, a.tariff
                        ORDER BY b.region, p.prov_name, a.bill_cycle, a.tariff";

                case SalesByTariffReportType.Region:
                    // Original: Select '',region,'',bill_cycle,tariff,sum(kwh_units)
                    //           group by 1,2,3,4,5 order by 1,2,3,4,5
                    // Note: typo in original SQL ('?) corrected to ?
                    return @"
                        SELECT '', b.region, '', a.bill_cycle, a.tariff,
                               sum(a.kwh_units)
                        FROM   account_info a, areas b, provinces p
                        WHERE  a.area_cd     = b.area_code
                          AND  (a.bill_cycle >= ? AND a.bill_cycle <= ?)
                          AND  b.prov_code   = p.prov_code
                          AND  a.tariff     NOT IN ('TM1')
                        GROUP BY b.region, a.bill_cycle, a.tariff
                        ORDER BY b.region, a.bill_cycle, a.tariff";

                case SalesByTariffReportType.EntireCEB:
                default:
                    // Original: Select '','Entire CEB','',bill_cycle,tariff,sum(kwh_units)
                    //           group by 1,2,3,4,5 order by 1,2,3,4,5
                    return @"
                        SELECT '', 'Entire CEB', '', a.bill_cycle, a.tariff,
                               sum(a.kwh_units)
                        FROM   account_info a, areas b, provinces p
                        WHERE  a.area_cd     = b.area_code
                          AND  (a.bill_cycle >= ? AND a.bill_cycle <= ?)
                          AND  b.prov_code   = p.prov_code
                          AND  a.tariff     NOT IN ('TM1')
                        GROUP BY a.bill_cycle, a.tariff
                        ORDER BY a.bill_cycle, a.tariff";
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────
        private string GetStringValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal)?.ToString()?.Trim();
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read string at ordinal {ordinal}");
                return null;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                if (reader.IsDBNull(ordinal)) return 0m;
                return Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read decimal at ordinal {ordinal}");
                return 0m;
            }
        }
    }
}