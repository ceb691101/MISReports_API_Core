using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.General.ActiveCustomersAndSalesTariff
{
    // ════════════════════════════════════════════════════════════════════════════
    //  ActiveCustomersOrdinaryDao
    //  Queries the ORDINARY Informix database (consmry / tariff_code / areas /
    //  provinces) to count active consumers grouped by tariff class.
    //  No location filter is applied – all areas / provinces / regions are returned.
    // ════════════════════════════════════════════════════════════════════════════
    public class ActiveCustomersOrdinaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, useBulkConnection: false);
        }

        /// <summary>
        /// Returns every (location | calc_cycle | tariff_class | count) row
        /// for the chosen report granularity and cycle range.
        /// </summary>
        public List<ActiveCustomersOrdinaryModel> GetActiveCustomersOrdinaryReport(ActiveCustomersRequest request)
        {
            var results = new List<ActiveCustomersOrdinaryModel>();

            try
            {
                logger.Info("=== START GetActiveCustomersOrdinaryReport ===");
                logger.Info($"ReportType={request.ReportType}, FromCycle={request.FromCycle}, ToCycle={request.ToCycle}");

                using (var conn = _dbConnection.GetConnection(useBulkConnection: false))
                {
                    conn.Open();

                    string sql = BuildOrdinarySql(request.ReportType);

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // All four queries share only the two cycle-range parameters
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
                                //   col 3 – sum(cnt)
                                var model = new ActiveCustomersOrdinaryModel
                                {
                                    BillCycle = GetStringValue(reader, 1),
                                    TariffClass = GetStringValue(reader, 2),
                                    Count = GetLongValue(reader, 3),
                                    ErrorMessage = string.Empty
                                };

                                // Map location key to the correct field(s) per report type
                                string locationKey = GetStringValue(reader, 0);
                                switch (request.ReportType)
                                {
                                    case ActiveCustomersReportType.Area:
                                        // col 0 = area_code, col 4 = area_name, col 5 = prov_name
                                        model.Area = GetStringValue(reader, 4);
                                        model.Province = GetStringValue(reader, 5);
                                        break;

                                    case ActiveCustomersReportType.Province:
                                        // col 0 = prov_code, col 4 = prov_name, col 5 = region
                                        model.Province = GetStringValue(reader, 4);
                                        model.Division = GetStringValue(reader, 5);
                                        break;

                                    case ActiveCustomersReportType.Region:
                                        // col 0 = a.Region (already the display value)
                                        model.Division = locationKey;
                                        break;

                                    case ActiveCustomersReportType.EntireCEB:
                                        // col 0 = literal 'Entire CEB' – nothing to map
                                        break;
                                }

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetActiveCustomersOrdinaryReport – {results.Count} rows ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetActiveCustomersOrdinaryReport");
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
        /// Parameters: (fromCycle, toCycle) – no location filter.
        /// </summary>
        private string BuildOrdinarySql(ActiveCustomersReportType reportType)
        {
            switch (reportType)
            {
                case ActiveCustomersReportType.Area:
                    // Original: Select c.area_code,c.calc_cycle,t.tariff_class,sum(c.cnt)
                    //           from consmry c, tariff_code t, areas a
                    //           where c.tariff_code=t.tariff_code
                    //             and (c.calc_cycle >=? and c.calc_cycle<=?)
                    //             and c.area_code=a.area_code
                    //           group by 1,2,3 order by 1,2,3
                    // Extra joined cols to resolve names without a second round-trip:
                    return @"
                        SELECT c.area_code, c.calc_cycle, t.tariff_class, sum(c.cnt),
                               a.area_name, p.prov_name
                        FROM   consmry c, tariff_code t, areas a, provinces p
                        WHERE  c.tariff_code = t.tariff_code
                          AND  (c.calc_cycle >= ? AND c.calc_cycle <= ?)
                          AND  c.area_code   = a.area_code
                          AND  a.prov_code   = p.prov_code
                        GROUP BY c.area_code, c.calc_cycle, t.tariff_class,
                                 a.area_name, p.prov_name
                        ORDER BY c.area_code, c.calc_cycle, t.tariff_class";

                case ActiveCustomersReportType.Province:
                    // Original: Select a.prov_code,c.calc_cycle,t.tariff_class,sum(c.cnt)
                    //           group by 1,2,3 order by 1,2,3
                    return @"
                        SELECT a.prov_code, c.calc_cycle, t.tariff_class, sum(c.cnt),
                               p.prov_name, a.region
                        FROM   consmry c, tariff_code t, areas a, provinces p
                        WHERE  c.tariff_code = t.tariff_code
                          AND  (c.calc_cycle >= ? AND c.calc_cycle <= ?)
                          AND  c.area_code   = a.area_code
                          AND  a.prov_code   = p.prov_code
                        GROUP BY a.prov_code, c.calc_cycle, t.tariff_class,
                                 p.prov_name, a.region
                        ORDER BY a.prov_code, c.calc_cycle, t.tariff_class";

                case ActiveCustomersReportType.Region:
                    // Original: Select a.Region,c.calc_cycle,t.tariff_class,sum(c.cnt)
                    //           group by 1,2,3 order by 1,2,3
                    return @"
                        SELECT a.Region, c.calc_cycle, t.tariff_class, sum(c.cnt)
                        FROM   consmry c, tariff_code t, areas a
                        WHERE  c.tariff_code = t.tariff_code
                          AND  (c.calc_cycle >= ? AND c.calc_cycle <= ?)
                          AND  c.area_code   = a.area_code
                        GROUP BY a.Region, c.calc_cycle, t.tariff_class
                        ORDER BY a.Region, c.calc_cycle, t.tariff_class";

                case ActiveCustomersReportType.EntireCEB:
                default:
                    // Original: Select 'Entire CEB',c.calc_cycle,t.tariff_class,sum(c.cnt)
                    //           group by 1,2,3 order by 1,2,3
                    return @"
                        SELECT 'Entire CEB', c.calc_cycle, t.tariff_class, sum(c.cnt)
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

        private long GetLongValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                if (reader.IsDBNull(ordinal)) return 0;
                return Convert.ToInt64(reader.GetValue(ordinal));
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read long at ordinal {ordinal}");
                return 0;
            }
        }
    }


    // ════════════════════════════════════════════════════════════════════════════
    //  ActiveCustomersBulkDao
    //  Queries the BULK Informix database (account_info / areas / provinces).
    //  TM1 is always excluded. No location filter – all entries are returned.
    // ════════════════════════════════════════════════════════════════════════════
    public class ActiveCustomersBulkDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, useBulkConnection: true);
        }

        /// <summary>
        /// Returns every (location | bill_cycle | tariff | count) row
        /// for the chosen report granularity and cycle range.
        /// </summary>
        public List<ActiveCustomersBulkModel> GetActiveCustomersBulkReport(ActiveCustomersRequest request)
        {
            var results = new List<ActiveCustomersBulkModel>();

            try
            {
                logger.Info("=== START GetActiveCustomersBulkReport ===");
                logger.Info($"ReportType={request.ReportType}, FromCycle={request.FromCycle}, ToCycle={request.ToCycle}");

                using (var conn = _dbConnection.GetConnection(useBulkConnection: true))
                {
                    conn.Open();

                    string sql = BuildBulkSql(request.ReportType);

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // All four queries share only the two cycle-range parameters
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
                                //   col 5 – sum(no_acc)
                                var model = new ActiveCustomersBulkModel
                                {
                                    Division = GetStringValue(reader, 0),
                                    Province = GetStringValue(reader, 1),
                                    BillCycle = GetStringValue(reader, 3),
                                    Tariff = GetStringValue(reader, 4),
                                    Count = GetLongValue(reader, 5),
                                    ErrorMessage = string.Empty
                                };

                                // Area report carries both the name and the code in col 2
                                if (request.ReportType == ActiveCustomersReportType.Area)
                                {
                                    model.Area = GetStringValue(reader, 2); // AREA_NAME
                                    model.AreaCode = GetStringValue(reader, 6); // area_cd (extra col)
                                }

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetActiveCustomersBulkReport – {results.Count} rows ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetActiveCustomersBulkReport");
                throw;
            }
        }

        /// <summary>
        /// Builds the bulk SELECT statement for the chosen granularity.
        ///
        /// Column order is always:
        ///   division | province | area_name | bill_cycle | tariff | sum(no_acc) [| area_cd for Area]
        ///
        /// Area     → prov_name | AREA_NAME | area_cd | bill_cycle | tariff | sum(no_acc)
        ///            (area_cd added as col 6 so frontend can group/sort)
        /// Province → region | prov_name | '' | bill_cycle | tariff | sum(no_acc)
        /// Region   → '' | region | '' | bill_cycle | tariff | sum(no_acc)
        /// EntireCEB→ '' | 'Entire CEB' | '' | bill_cycle | tariff | sum(no_acc)
        ///
        /// Parameters: (fromCycle, toCycle) – no location filter.
        /// TM1 excluded in all variants.
        /// </summary>
        private string BuildBulkSql(ActiveCustomersReportType reportType)
        {
            switch (reportType)
            {
                case ActiveCustomersReportType.Area:
                    // Original: Select p.prov_name,AREA_NAME,area_cd,bill_cycle,tariff,sum(no_acc)
                    //           from account_info a,areas b, provinces p
                    //           where a.area_cd = b.area_code And (bill_cycle >= ? And bill_cycle <= ?)
                    //             and b.prov_code=p.prov_code and tariff not in ('TM1')
                    //           group by 1,2,3,4,5 order by 1,2,3,4,5
                    // col 0 = prov_name (used as "division" slot to keep layout consistent)
                    // col 1 = AREA_NAME, col 2 already = area_cd, col 6 = area_cd duplicate for AreaCode
                    return @"
                        SELECT p.prov_name, b.AREA_NAME, a.area_cd, a.bill_cycle, a.tariff,
                               sum(a.no_acc), a.area_cd
                        FROM   account_info a, areas b, provinces p
                        WHERE  a.area_cd     = b.area_code
                          AND  (a.bill_cycle >= ? AND a.bill_cycle <= ?)
                          AND  b.prov_code   = p.prov_code
                          AND  a.tariff     NOT IN ('TM1')
                        GROUP BY p.prov_name, b.AREA_NAME, a.area_cd, a.bill_cycle, a.tariff
                        ORDER BY p.prov_name, b.AREA_NAME, a.area_cd, a.bill_cycle, a.tariff";

                case ActiveCustomersReportType.Province:
                    // Original: Select region,p.prov_name,'',bill_cycle,tariff,sum(no_acc)
                    //           group by 1,2,3,4,5 order by 1,2,3,4,5
                    return @"
                        SELECT b.region, p.prov_name, '', a.bill_cycle, a.tariff,
                               sum(a.no_acc)
                        FROM   account_info a, areas b, provinces p
                        WHERE  a.area_cd     = b.area_code
                          AND  (a.bill_cycle >= ? AND a.bill_cycle <= ?)
                          AND  b.prov_code   = p.prov_code
                          AND  a.tariff     NOT IN ('TM1')
                        GROUP BY b.region, p.prov_name, a.bill_cycle, a.tariff
                        ORDER BY b.region, p.prov_name, a.bill_cycle, a.tariff";

                case ActiveCustomersReportType.Region:
                    // Original: Select '',region,'',bill_cycle,tariff,sum(no_acc)
                    //           group by 1,2,3,4,5 order by 1,2,3,4,5
                    return @"
                        SELECT '', b.region, '', a.bill_cycle, a.tariff,
                               sum(a.no_acc)
                        FROM   account_info a, areas b, provinces p
                        WHERE  a.area_cd     = b.area_code
                          AND  (a.bill_cycle >= ? AND a.bill_cycle <= ?)
                          AND  b.prov_code   = p.prov_code
                          AND  a.tariff     NOT IN ('TM1')
                        GROUP BY b.region, a.bill_cycle, a.tariff
                        ORDER BY b.region, a.bill_cycle, a.tariff";

                case ActiveCustomersReportType.EntireCEB:
                default:
                    // Original: Select '','Entire CEB','',bill_cycle,tariff,sum(no_acc)
                    //           group by 1,2,3,4,5 order by 1,2,3,4,5
                    return @"
                        SELECT '', 'Entire CEB', '', a.bill_cycle, a.tariff,
                               sum(a.no_acc)
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

        private long GetLongValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                if (reader.IsDBNull(ordinal)) return 0;
                return Convert.ToInt64(reader.GetValue(ordinal));
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not read long at ordinal {ordinal}");
                return 0;
            }
        }
    }
}