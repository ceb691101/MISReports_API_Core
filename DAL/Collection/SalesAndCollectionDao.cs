using MISReports_Api.DBAccess;
using MISReports_Api.Models.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Collection.SalesAndCollection
{
    /// <summary>
    /// Sales &amp; Collection – Region Wise report DAO.
    ///
    /// Two-pass strategy per row:
    ///   Pass 1 – ordinary DB (bill_type='O'):  get area rows + ord_sup + ord_collect
    ///   Pass 2 – bulk DB    (bill_type='B'):  match by area_name → bulk_sup + bulk_collect
    ///
    /// Computed per area:
    ///   ord_sup    = rs[3] + rs[6] - rs[7]
    ///   bulk_sup   = dr[3] + dr[6] - dr[7]
    ///   net_sale   = ord_sup + bulk_sup
    ///   net_collect = ord_collect + bulk_collect
    ///   % collect  = net_collect / net_sale * 100
    /// </summary>
    public class SalesAndCollectionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false); // ordinary DB
        }

        // ── Main entry point ──────────────────────────────────────────────────
        public List<SalesAndCollectionModel> GetSalesAndCollectionReport(SalesAndCollectionRequest request)
        {
            logger.Info($"=== START GetSalesAndCollectionReport === BillCycle={request.BillCycle} " +
            $"ReportType={request.ReportType} Province={request.ProvinceCode} Region={request.RegionCode}");
            try
            {
                // Pass 1: fetch ordinary rows
                var ordinaryRows = GetOrdinaryRows(request);

                if (ordinaryRows.Count == 0)
                {
                    logger.Warn("No ordinary rows returned — returning empty result.");
                    return new List<SalesAndCollectionModel>();
                }

                // Pass 2: for each area, query bulk DB and merge
                var results = MergeBulkData(request.BillCycle, ordinaryRows);

                logger.Info($"=== END GetSalesAndCollectionReport === {results.Count} rows ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetSalesAndCollectionReport");
                throw;
            }
        }

        // ── Pass 1: Ordinary DB ───────────────────────────────────────────────
        private List<SalesAndCollectionModel> GetOrdinaryRows(SalesAndCollectionRequest request)
        {
            var rows = new List<SalesAndCollectionModel>();
            string sql = BuildOrdinarySQL(request);

            logger.Info($"Ordinary SQL ReportType={request.ReportType}: {sql}");

            try
            {
                using (var conn = _dbConnection.GetConnection(false)) // ordinary
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // Bind parameters in the order they appear
                        cmd.Parameters.AddWithValue("?", request.BillCycle);

                        if (request.ReportType == SalesCollectionReportType.Region)
                            cmd.Parameters.AddWithValue("?", request.RegionCode.Trim());
                        else if (request.ReportType == SalesCollectionReportType.Province)
                            cmd.Parameters.AddWithValue("?", request.ProvinceCode.Trim());

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                decimal monChg = GetDecimalByName(reader, "mon_chg");
                                decimal un_chg = GetDecimalByName(reader, "un_chg");
                                decimal ov_chg = GetDecimalByName(reader, "ov_chg");
                                decimal payments = GetDecimalByName(reader, "payments");
                                string pCode = GetStringByName(reader, "prov_code");
                                string aName = GetStringByName(reader, "area_name");
                                string aCode = GetStringByName(reader, "area_code");

                                var row = new SalesAndCollectionModel
                                {
                                    ProvinceCode = pCode,
                                    AreaCode = aCode,
                                    AreaName = aName,
                                    RawOrdinarySupply = monChg + un_chg - ov_chg,
                                    RawOrdinaryCollection = payments
                                };

                                rows.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching ordinary rows");
                throw;
            }

            logger.Info($"Ordinary pass returned {rows.Count} area rows");
            return rows;
        }

        // ── Pass 2: Bulk DB per area, then merge ──────────────────────────────
        private List<SalesAndCollectionModel> MergeBulkData(string billCycle,
            List<SalesAndCollectionModel> ordinaryRows)
        {
            const string bulkSql =
                "SELECT r.mon_chg, r.un_chg, r.ov_chg, r.payments " +
                "FROM receive_position r, areas a " +
                "WHERE r.bill_cycle = ? AND r.bill_type = 'B' " +
                "AND r.area_code = ? AND a.area_code = r.area_code";

            try
            {
                using (var conn = _dbConnection.GetConnection(false)) // ordinary summary DB
                {
                    conn.Open();

                    foreach (var row in ordinaryRows)
                    {
                        logger.Info($"Bulk pass: querying area_code='{row.AreaCode}' area_name='{row.AreaName}'");
                        try
                        {
                            using (var cmd = new OleDbCommand(bulkSql, conn))
                            {
                                cmd.Parameters.AddWithValue("?", billCycle);
                                cmd.Parameters.AddWithValue("?", row.AreaCode);

                                using (var reader = cmd.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        decimal monChg = GetDecimalByName(reader, "mon_chg");
                                        decimal un_chg = GetDecimalByName(reader, "un_chg");
                                        decimal ov_chg = GetDecimalByName(reader, "ov_chg");
                                        decimal payments = GetDecimalByName(reader, "payments");

                                        row.RawBulkSupply = monChg + un_chg - ov_chg;
                                        row.RawBulkCollection = payments;
                                    }
                                    // If no bulk row exists, bulk fields remain 0
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Warn(ex, $"Bulk query failed for area '{row.AreaName}' — defaulting to 0");
                            row.RawBulkSupply = 0;
                            row.RawBulkCollection = 0;
                        }

                        // Format all display fields now that both passes are done
                        FormatRow(row);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error opening bulk connection for merge pass");
                // Still format with whatever ordinary data we have
                foreach (var row in ordinaryRows) FormatRow(row);
            }

            return ordinaryRows;
        }

        // ── SQL builders ──────────────────────────────────────────────────────
        private static string BuildOrdinarySQL(SalesAndCollectionRequest request)
        {
            switch (request.ReportType)
            {
                case SalesCollectionReportType.Region:
                    return
                        "SELECT r.mon_chg, r.un_chg, r.ov_chg, r.payments, a.prov_code, a.area_name, r.area_code " +
                        "FROM receive_position r, areas a, provinces p " +
                        "WHERE r.bill_cycle = ? AND r.bill_type = 'O' " +
                        "AND r.area_code = a.area_code AND a.area_code = r.area_code " +
                        "AND a.prov_code = p.prov_code " +
                        "AND a.region = ? " +
                        "AND a.bill_cycle IS NOT NULL " +
                        "ORDER BY a.prov_code, a.area_name";

                case SalesCollectionReportType.Province:
                    return
                        "SELECT r.mon_chg, r.un_chg, r.ov_chg, r.payments, a.prov_code, a.area_name, r.area_code " +
                        "FROM receive_position r, areas a, provinces p " +
                        "WHERE r.bill_cycle = ? AND r.bill_type = 'O' " +
                        "AND r.area_code = a.area_code AND a.area_code = r.area_code " +
                        "AND a.prov_code = p.prov_code " +
                        "AND p.prov_code = ? " +
                        "AND a.bill_cycle IS NOT NULL " +
                        "ORDER BY a.area_name";

                default: // EntireCEB
                    return
                        "SELECT r.mon_chg, r.un_chg, r.ov_chg, r.payments, a.prov_code, a.area_name, r.area_code " +
                        "FROM receive_position r, areas a, provinces p " +
                        "WHERE r.bill_cycle = ? AND r.bill_type = 'O' " +
                        "AND r.area_code = a.area_code AND a.area_code = r.area_code " +
                        "AND a.prov_code = p.prov_code " +
                        "AND a.bill_cycle IS NOT NULL " +
                        "ORDER BY a.area_name";
            }
        }

        // ── Formatting ────────────────────────────────────────────────────────
        private static void FormatRow(SalesAndCollectionModel row)
        {
            row.OrdinarySupply = FormatDecimal(row.RawOrdinarySupply);
            row.BulkSupply = FormatDecimal(row.RawBulkSupply);
            row.TotalNetSales = FormatDecimal(row.RawTotalNetSales);
            row.OrdinaryCollection = FormatDecimal(row.RawOrdinaryCollection);
            row.BulkCollection = FormatDecimal(row.RawBulkCollection);
            row.TotalCollections = FormatDecimal(row.RawTotalCollections);
            row.CollectionPercentage = row.RawCollectionPercentage.ToString("0.00") + "%";
        }

        private static string FormatDecimal(decimal value)
        {
            try { return value.ToString("###,###,###.00"); }
            catch { return "0.00"; }
        }

        // ── Reader helpers ────────────────────────────────────────────────────
        private static decimal GetDecimal(OleDbDataReader reader, int ordinal)
        {
            try { return reader.IsDBNull(ordinal) ? 0 : Convert.ToDecimal(reader.GetValue(ordinal)); }
            catch { return 0; }
        }

        private static string GetString(OleDbDataReader reader, int ordinal)
        {
            try { return reader.IsDBNull(ordinal) ? "" : reader.GetString(ordinal).Trim(); }
            catch { return ""; }
        }

        private static string GetStringByName(OleDbDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? "" : reader.GetValue(ordinal).ToString().Trim();
            }
            catch { return ""; }
        }

        private static decimal GetDecimalByName(OleDbDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch { return 0; }
        }
    }
}