using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAL.General.ArrearsPosition
{
    /// <summary>
    /// Fetches the Arrears Position (meter-reader wise) report from the
    /// billsmry database.
    ///
    /// Business logic ported from the legacy VB report:
    ///
    ///   Step 1 – For each reader, query prn_dat_1 to get:
    ///              kwhCharge  = SUM(kwh_charge + fuel_charge)
    ///              CrntBalance= SUM(crnt_balance)
    ///              ReaderCount= COUNT(reader_code)
    ///
    ///   Step 2 – For each reader, query prn_dat_1 JOIN prn_dat_2 to get:
    ///              TrnsAmount = SUM(transac_amt) WHERE transac_code LIKE 'NR'
    ///
    ///   Step 3 – Derive:
    ///              charge = kwhCharge - TrnsAmount
    ///              ratio  = (charge != 0) ? CrntBalance / charge : 0
    ///
    /// DB     : billsmry
    /// Tables : prn_dat_1, prn_dat_2
    /// </summary>
    public class ArrearsPositionDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // bulk connection
        }

        public List<ArrearsPositionModel> GetArrearsPositionReport(ArrearsPositionRequest request)
        {
            var results = new List<ArrearsPositionModel>();

            try
            {
                logger.Info("=== START GetArrearsPositionReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, AreaCode={request.AreaCode}");

                using (var conn = _dbConnection.GetConnection(false)) // bulk connection
                {
                    if (conn.State != ConnectionState.Open)
                        conn.Open();

                    // ── Step 1: grouped reader data from prn_dat_1 ──────────────
                    string readerSql = @"SELECT   reader_code,
                                                  SUM(kwh_charge + fuel_charge),
                                                  SUM(crnt_balance),
                                                  COUNT(reader_code)
                                         FROM     prn_dat_1
                                         WHERE    bill_cycle = ?
                                           AND    area_code  = ?
                                         GROUP BY reader_code
                                         ORDER BY reader_code";

                    // ── Step 2: NR transaction total per reader ──────────────────
                    string trnsAmtSql = @"SELECT SUM(t.transac_amt)
                                          FROM   prn_dat_1 p,
                                                 prn_dat_2 t
                                          WHERE  t.bill_cycle   = ?
                                            AND  p.area_code    = ?
                                            AND  p.reader_code  = ?
                                            AND  t.transac_code LIKE 'NR'
                                            AND  p.area_code    = t.area_code
                                            AND  p.acct_number  = t.acct_number";

                    logger.Info($"Executing reader SQL for BillCycle={request.BillCycle}, AreaCode={request.AreaCode}");

                    using (var readerCmd = new OleDbCommand(readerSql, conn))
                    {
                        readerCmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                        readerCmd.Parameters.AddWithValue("@area_code", request.AreaCode);

                        using (var dr = readerCmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                // ── Step 1 values ──────────────────────────────
                                string readerCode = dr.IsDBNull(0) ? "" : dr.GetString(0).Trim();
                                decimal kwhCharge = dr.IsDBNull(1) ? 0m : Convert.ToDecimal(dr.GetValue(1));
                                decimal crntBalance = dr.IsDBNull(2) ? 0m : Convert.ToDecimal(dr.GetValue(2));
                                int readerCount = dr.IsDBNull(3) ? 0 : Convert.ToInt32(dr.GetValue(3));

                                // ── Step 2: NR transaction amount for this reader
                                decimal trnsAmount = 0m;
                                using (var trnsCmd = new OleDbCommand(trnsAmtSql, conn))
                                {
                                    trnsCmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                                    trnsCmd.Parameters.AddWithValue("@area_code", request.AreaCode);
                                    trnsCmd.Parameters.AddWithValue("@reader_code", readerCode);

                                    object trnsResult = trnsCmd.ExecuteScalar();

                                    // Legacy: "If objdb_connection.dr(0) Is Value Then TrnsAmount = 0"
                                    if (trnsResult != null && trnsResult != DBNull.Value)
                                        trnsAmount = Convert.ToDecimal(trnsResult);
                                }

                                // ── Step 3: derive charge and ratio ───────────
                                decimal charge = kwhCharge - trnsAmount;
                                decimal ratio = (charge != 0m) ? crntBalance / charge : 0m;

                                // ── Build model ────────────────────────────────
                                var model = new ArrearsPositionModel
                                {
                                    ReaderCode = readerCode,
                                    RawCharge = charge,
                                    RawCrntBalance = crntBalance,
                                    RawRatio = ratio,
                                    RawReaderCount = readerCount,

                                    // Format strings match legacy VB exactly
                                    Charge = FormatAmount(charge),       // ###,###,###.#0
                                    CrntBalance = FormatAmount(crntBalance),  // ###,###,###.#0
                                    Ratio = FormatRatio(ratio),         // ##0.#0
                                    ReaderCount = FormatCount(readerCount),   // ###,###,##0

                                    ErrorMessage = string.Empty
                                };

                                results.Add(model);
                            }
                        }
                    }

                    logger.Info($"=== END GetArrearsPositionReport (Success) - {results.Count} records ===");
                }

                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching arrears position report");
                throw;
            }
        }

        // ── Formatting helpers (mirror legacy VB format strings) ───────────────

        /// <summary>Format: ###,###,###.#0 (two decimal places, comma-grouped)</summary>
        private string FormatAmount(decimal value)
        {
            try { return value.ToString("#,##0.00"); }
            catch { return "0.00"; }
        }

        /// <summary>Format: ##0.#0 (ratio, two decimal places)</summary>
        private string FormatRatio(decimal value)
        {
            try { return value.ToString("##0.00"); }
            catch { return "0.00"; }
        }

        /// <summary>Format: ###,###,##0 (integer, comma-grouped)</summary>
        private string FormatCount(int value)
        {
            try { return value.ToString("#,##0"); }
            catch { return "0"; }
        }
    }
}