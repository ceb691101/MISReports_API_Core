using MISReports_Api.Compat;
using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace MISReports_Api.DAL.SolarInformation
{
    public class SolarRooftopPaymentsandExportUnitsDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage, string customerType)
        {
            bool useBulkConnection = string.Equals(customerType, "Bulk", StringComparison.OrdinalIgnoreCase);
            return _dbConnection.TestConnection(out errorMessage, useBulkConnection);
        }

        public List<SolarRooftopPaymentsandExportUnitsModel> GetReport(SolarRooftopPaymentsandExportUnitsRequest request)
        {
            var results = new List<SolarRooftopPaymentsandExportUnitsModel>();

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            bool useBulkConnection = string.Equals(request.CustomerType, "Bulk", StringComparison.OrdinalIgnoreCase);

            try
            {
              logger.Info($"=== START GetReport for {request.CustomerType} - {request.ReportType} ===");

              using (var conn = _dbConnection.GetConnection(useBulkConnection))
                {
                    conn.Open();

                    string sql = BuildReportQuery(request);
                logger.Debug($"Report SQL: {sql}");

                    using (var cmd = Informix.CreateCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300;
                        AddParameters(cmd, request);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                results.Add(MapReport(reader, request));
                            }
                        }
                    }
                }

                logger.Info($"=== END GetReport - {results.Count} records ===");
                return results;
            }
            catch (DbException ex)
            {
                logger.Error(ex, "Error occurred while fetching solar rooftop report data");
                throw new Exception("Error retrieving solar rooftop report data: " + ex.Message, ex);
            }
        }

        private SolarRooftopPaymentsandExportUnitsModel MapReport(
            DbDataReader reader,
            SolarRooftopPaymentsandExportUnitsRequest request)
        {
          bool isBulk = string.Equals(request.CustomerType, "Bulk", StringComparison.OrdinalIgnoreCase);

            return new SolarRooftopPaymentsandExportUnitsModel
            {
                NetType = GetColumnValue(reader, "net_type"),
                Province = request.Province,
                Region = GetColumnValue(reader, "region") ?? request.Region,
                Area = request.Area,
                TariffCategory = GetColumnValue(reader, "tariff_cat"),
            TariffCode = isBulk ? GetColumnValue(reader, "tariff") : GetColumnValue(reader, "tariff_code"),
                CalcCycle = GetColumnValue(reader, "calc_cycle"),
                BillCycle = GetColumnValue(reader, "bill_cycle") ?? request.BillCycle,
            Rate = isBulk ? GetDecimalValueAt(reader, 2) : GetDecimalValue(reader, "rate"),
            Units = isBulk ? GetDecimalValueAt(reader, 3) : GetDecimalValue(reader, "units"),
            PaymentAmount = isBulk
              ? (request.ReportType == SolarRooftopReportType.Payments ? GetDecimalValueAt(reader, 4) : 0)
              : GetDecimalValue(reader, "pdAmt"),
            ExportUnits = isBulk
              ? (request.ReportType == SolarRooftopReportType.ExportUnits ? GetDecimalValueAt(reader, 3) : 0)
              : (request.ReportType == SolarRooftopReportType.ExportUnits ? GetDecimalValue(reader, "units") : 0),
                ErrorMessage = string.Empty
            };
        }

        private string BuildReportQuery(SolarRooftopPaymentsandExportUnitsRequest request)
        {
            bool isBulk = string.Equals(request.CustomerType, "Bulk", StringComparison.OrdinalIgnoreCase);
            bool isExport = request.ReportType == SolarRooftopReportType.ExportUnits;
            // Swagger accepts either a province code or its display name.
            string scopePredicate = string.IsNullOrWhiteSpace(request.Region)
                ? @"(a.prov_code = ? OR a.prov_code IN
                    (SELECT p.prov_code FROM provinces p
                     WHERE UPPER(TRIM(p.prov_name)) = UPPER(?)))"
                : "a.region = ?";

            if (isBulk)
            {
                if (isExport)
                {
                // Bulk - Export Units.
                    return $@"SELECT 'Net Accounting' AS net_type, n.tariff, n.rate, SUM(n.exp_kwd_units)
                            FROM netmtcons n, areas a
                            WHERE n.bill_cycle = ?
                              AND n.net_type IN ('2')
                              AND n.rate <> 0
                              AND a.area_code = n.area_cd
                              AND {scopePredicate}
                            GROUP BY 1,2,3

                            UNION ALL

                            SELECT 'Net Plus' AS net_type, n.tariff, n.rate, SUM(n.exp_kwd_units)
                            FROM netmtcons n, areas a
                            WHERE n.bill_cycle = ?
                              AND n.net_type IN ('3')
                              AND n.rate <> 0
                              AND a.area_code = n.area_cd
                              AND {scopePredicate}
                            GROUP BY 1,2,3

                            UNION ALL

                            SELECT 'Net Plus Plus' AS net_type, n.tariff, n.rate, SUM(n.exp_kwd_units)
                            FROM netmtcons n, areas a
                            WHERE n.bill_cycle = ?
                              AND n.net_type IN ('4')
                              AND n.rate <> 0
                              AND a.area_code = n.area_cd
                              AND {scopePredicate}
                            GROUP BY 1,2,3
                            ORDER BY 1,2,3,4";
                }

                // Bulk - Payments.
                return $@"SELECT 'Net Accounting' AS net_type,n.tariff,n.rate, SUM(n.unitsale), SUM(n.kwh_sales)
                        FROM netmtcons n, areas a
                        WHERE n.bill_cycle = ?
                          AND n.net_type IN ('2')
                          AND n.rate <> 0
                          AND a.area_code = n.area_cd
                          AND {scopePredicate}
                        GROUP BY 1,2,3

                        UNION ALL

                           SELECT 'Net Plus' AS net_type,n.tariff,n.rate, SUM(n.unitsale), SUM(n.kwh_sales)
                        FROM netmtcons n, areas a
                        WHERE n.bill_cycle = ?
                          AND n.net_type IN ('3')
                          AND n.rate <> 0
                          AND a.area_code = n.area_cd
                          AND {scopePredicate}
                        GROUP BY 1,2,3

                        UNION ALL

                           SELECT 'Net Plus Plus' AS net_type,n.tariff,n.rate, SUM(n.unitsale), SUM(n.kwh_sales)
                        FROM netmtcons n, areas a
                        WHERE n.bill_cycle = ?
                          AND n.net_type IN ('4')
                          AND n.rate <> 0
                          AND a.area_code = n.area_cd
                          AND {scopePredicate}
                        GROUP BY 1,2,3
                        ORDER BY 1,2,3,4,5";
            }

            if (isExport)
            {
              // Ordinary - Export Units.
                return $@"SELECT 'Net Accounting' AS net_type, a.region, n.calc_cycle, c.tariff_cat, n.tariff_code AS tariff_code, n.rate,
                       SUM(units_out) AS units
                        FROM netmtcons n, areas a, cat_tariff_table c
                        WHERE n.calc_cycle = ?
                          AND n.net_type IN ('2','5')
                          AND n.rate <> 0
                          AND a.area_code = n.area_code
                          AND c.cus_cat = 'O'
                          AND c.tariff_code = n.tariff_code
                          AND {scopePredicate}
                        GROUP BY a.region, n.calc_cycle, c.tariff_cat, n.tariff_code, n.rate

                        UNION ALL

                        SELECT 'Net Plus' AS net_type, a.region, n.calc_cycle, c.tariff_cat, n.tariff_code AS tariff_code, n.rate,
                             SUM(units_out) AS units
                        FROM netmtcons n, areas a, cat_tariff_table c
                        WHERE n.calc_cycle = ?
                          AND n.net_type IN ('3')
                          AND n.rate <> 0
                          AND a.area_code = n.area_code
                          AND c.cus_cat = 'O'
                          AND c.tariff_code = n.tariff_code
                          AND {scopePredicate}
                        GROUP BY a.region, n.calc_cycle, c.tariff_cat, n.tariff_code, n.rate

                        UNION ALL

                        SELECT 'Net Plus Plus' AS net_type, a.region, n.calc_cycle, c.tariff_cat, n.tariff_code AS tariff_code, n.rate,
                             SUM(units_out) AS units
                        FROM netmtcons n, areas a, cat_tariff_table c
                        WHERE n.calc_cycle = ?
                          AND n.net_type IN ('4')
                          AND n.rate <> 0
                          AND a.area_code = n.area_code
                          AND c.cus_cat = 'O'
                          AND c.tariff_code = n.tariff_code
                          AND {scopePredicate}
                        GROUP BY a.region, n.calc_cycle, c.tariff_cat, n.tariff_code, n.rate
                        ORDER BY 1,2,3,4,5,6";
            }

            // Ordinary - Payments.
            return $@"SELECT 'Net Accounting' AS net_type, a.region, n.calc_cycle, c.tariff_cat, n.tariff_code AS tariff_code, n.rate,
                           SUM(unitsale) AS units,
                           SUM(kwh_sales) AS pdAmt
                    FROM netmtcons n, areas a, cat_tariff_table c
                    WHERE n.calc_cycle = ?
                      AND n.net_type IN ('2','5')
                      AND n.rate <> 0
                      AND a.area_code = n.area_code
                      AND c.cus_cat = 'O'
                      AND c.tariff_code = n.tariff_code
                      AND {scopePredicate}
                    GROUP BY a.region, n.calc_cycle, c.tariff_cat, n.tariff_code, n.rate

                    UNION ALL

                    SELECT 'Net Plus' AS net_type, a.region, n.calc_cycle, c.tariff_cat, n.tariff_code AS tariff_code, n.rate,
                           SUM(unitsale) AS units,
                            SUM(kwh_sales) AS pdAmt
                    FROM netmtcons n, areas a, cat_tariff_table c
                    WHERE n.calc_cycle = ?
                      AND n.net_type IN ('3')
                      AND n.rate <> 0
                      AND a.area_code = n.area_code
                      AND c.cus_cat = 'O'
                      AND c.tariff_code = n.tariff_code
                      AND {scopePredicate}
                    GROUP BY a.region, n.calc_cycle, c.tariff_cat, n.tariff_code, n.rate

                    UNION ALL

                    SELECT 'Net Plus Plus' AS net_type, a.region, n.calc_cycle, c.tariff_cat, n.tariff_code AS tariff_code, n.rate,
                           SUM(unitsale) AS units,
                            SUM(kwh_sales) AS pdAmt
                    FROM netmtcons n, areas a, cat_tariff_table c
                    WHERE n.calc_cycle = ?
                      AND n.net_type IN ('4')
                      AND n.rate <> 0
                      AND a.area_code = n.area_code
                      AND c.cus_cat = 'O'
                      AND c.tariff_code = n.tariff_code
                      AND {scopePredicate}
                    GROUP BY a.region, n.calc_cycle, c.tariff_cat, n.tariff_code, n.rate
                    ORDER BY 1,2,3,4,5,6";
        }

        private void AddParameters(DbCommand cmd, SolarRooftopPaymentsandExportUnitsRequest request)
        {
            var isBulk = string.Equals(request.CustomerType, "Bulk", StringComparison.OrdinalIgnoreCase);
            var filterValue = !string.IsNullOrWhiteSpace(request.Region) ? request.Region : request.Province;

            if (string.IsNullOrWhiteSpace(filterValue))
                throw new ArgumentException("Province or division is required.", nameof(request));

            bool isProvince = string.IsNullOrWhiteSpace(request.Region);
            // OleDb binds by position: repeat cycle, code, and (for provinces) name
            // in the same order for each of the three UNION branches.
            for (int branch = 1; branch <= 3; branch++)
            {
                cmd.Parameters.AddWithValue(isBulk ? "@billCycle" + branch : "@calcCycle" + branch, request.BillCycle);
                cmd.Parameters.AddWithValue("@filter" + branch, filterValue.Trim());
                if (isProvince)
                    cmd.Parameters.AddWithValue("@provinceName" + branch, filterValue.Trim());
            }
        }

        private string GetColumnValue(DbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? null : value.ToString()?.Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return null;
            }
        }

        private decimal GetDecimalValue(DbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column '{columnName}'");
                return 0;
            }
        }

        private decimal GetDecimalValueAt(DbDataReader reader, int ordinal)
        {
          try
          {
            var value = reader[ordinal];
            return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
          }
          catch (Exception ex)
          {
            logger.Warn(ex, $"Invalid decimal format in column ordinal '{ordinal}'");
            return 0;
          }
        }
    }
}
