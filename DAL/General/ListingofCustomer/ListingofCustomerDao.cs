using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Text;

namespace MISReports_Api.DAL.General.ListingOfCustomer
{
    /// <summary>
    /// Data-access layer for the Listing of Customers report.
    /// Mirrors the patterns used in SolarReadingUsageBulkDao / ListOfGovernmentAccountsDao.
    /// Uses the ORDINARY (non-bulk) connection — prn_dat_1 is an ordinary table.
    /// </summary>
    public class ListingOfCustomerDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // ── Connection health-check ──────────────────────────────────────────
        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false); // ordinary connection
        }

        // ════════════════════════════════════════════════════════════════════
        //  Max Bill Cycle
        // ════════════════════════════════════════════════════════════════════

      

        // ════════════════════════════════════════════════════════════════════
        //  Filter Dropdowns
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Loads every dropdown list in a single connection so the frontend
        /// can populate all filter controls after area + bill cycle are chosen.
        /// </summary>
        public ListingOfCustomerFiltersModel GetFilters(string areaCode, string billCycle)
        {
            var model = new ListingOfCustomerFiltersModel { BillCycle = billCycle };

            try
            {
                logger.Info($"Getting filters for area={areaCode}, billCycle={billCycle}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    model.Tariffs = GetDistinctValues(conn, areaCode, billCycle, "tariff_code");
                    model.Transformers = GetDistinctValues(conn, areaCode, billCycle, "substn_code");
                    model.Phases = GetDistinctValues(conn, areaCode, billCycle, "no_of_phase");
                    model.ConnectionTypes = GetDistinctValues(conn, areaCode, billCycle, "conect_type");
                    model.ReaderCodes = GetDistinctValues(conn, areaCode, billCycle, "reader_code");
                    model.DailyPacks = GetDistinctValues(conn, areaCode, billCycle, "dly_pack_no");
                    model.Depots = GetDistinctValues(conn, areaCode, billCycle, "crnt_depot");
                }

                logger.Info("Filters loaded successfully");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error loading filters");
                model.ErrorMessage = ex.Message;
            }

            return model;
        }

        // ════════════════════════════════════════════════════════════════════
        //  Main Report
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the customer listing for the supplied filters.
        /// The WHERE clause is built dynamically, exactly as in the legacy VB code,
        /// but uses parameterised queries to prevent SQL injection.
        /// </summary>
        public List<ListingOfCustomerModel> GetListingOfCustomerReport(ListingOfCustomerRequest request)
        {
            var results = new List<ListingOfCustomerModel>();

            try
            {
                logger.Info("=== START GetListingOfCustomerReport ===");
                logger.Info($"Request: AreaCode={request.AreaCode}, BillCycle={request.BillCycle}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Build WHERE clause and matching parameter list
                    var parameters = new List<object>();
                    string sql = BuildReportSql(request, parameters);

                    logger.Info($"Executing SQL with {parameters.Count} parameters");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        foreach (var p in parameters)
                            cmd.Parameters.AddWithValue("?", p);

                        using (var reader = cmd.ExecuteReader())
                        {
                            // Resolve area name once (reuse connection)
                            string areaName = GetAreaName(conn, request.AreaCode);

                            while (reader.Read())
                            {
                                var model = MapReaderToModel(reader);
                                model.AreaCode = request.AreaCode;
                                model.AreaName = areaName;
                                model.BillCycle = request.BillCycle;
                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetListingOfCustomerReport (Success) — {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching listing of customer report");
                throw;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  Private helpers
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Builds the parameterised SQL and populates the parameters list.
        /// Mirrors the legacy whrStr construction but is injection-safe.
        /// </summary>
        private string BuildReportSql(ListingOfCustomerRequest req, List<object> parameters)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT * FROM prn_dat_1 WHERE area_code = ? AND bill_cycle = ?");
            parameters.Add(req.AreaCode);
            parameters.Add(req.BillCycle);

            if (req.UseTariff && !string.IsNullOrWhiteSpace(req.Tariff))
            {
                sb.Append(" AND tariff_code = ?");
                parameters.Add(req.Tariff);
                logger.Info($"Filter: tariff_code = {req.Tariff}");
            }

            if (req.UseTransformer && !string.IsNullOrWhiteSpace(req.Transformer))
            {
                sb.Append(" AND substn_code = ?");
                parameters.Add(req.Transformer);
                logger.Info($"Filter: substn_code = {req.Transformer}");
            }

            if (req.UsePhase && !string.IsNullOrWhiteSpace(req.Phase))
            {
                sb.Append(" AND no_of_phase = ?");
                parameters.Add(req.Phase);
                logger.Info($"Filter: no_of_phase = {req.Phase}");
            }

            if (req.UseConnectionType && !string.IsNullOrWhiteSpace(req.ConnectionType))
            {
                sb.Append(" AND conect_type = ?");
                parameters.Add(req.ConnectionType);
                logger.Info($"Filter: conect_type = {req.ConnectionType}");
            }

            if (req.UseReaderCode && !string.IsNullOrWhiteSpace(req.ReaderCode))
            {
                sb.Append(" AND reader_code = ?");
                parameters.Add(req.ReaderCode);
                logger.Info($"Filter: reader_code = {req.ReaderCode}");
            }

            if (req.UseDailyPack && !string.IsNullOrWhiteSpace(req.DailyPackNo))
            {
                sb.Append(" AND dly_pack_no = ?");
                parameters.Add(req.DailyPackNo);
                logger.Info($"Filter: dly_pack_no = {req.DailyPackNo}");
            }

            if (req.UseDepot && !string.IsNullOrWhiteSpace(req.Depot))
            {
                sb.Append(" AND crnt_depot = ?");
                parameters.Add(req.Depot);
                logger.Info($"Filter: crnt_depot = {req.Depot}");
            }

            if (req.UseBalance
                && !string.IsNullOrWhiteSpace(req.BalanceOperator)
                && !string.IsNullOrWhiteSpace(req.BalanceAmount)
                && IsValidOperator(req.BalanceOperator)
                && decimal.TryParse(req.BalanceAmount, out _))
            {
                // Operator is validated above — safe to interpolate
                sb.Append($" AND crnt_balance {req.BalanceOperator} ?");
                parameters.Add(decimal.Parse(req.BalanceAmount));
                logger.Info($"Filter: crnt_balance {req.BalanceOperator} {req.BalanceAmount}");
            }

            if (req.UseLastPaymentDate
                && !string.IsNullOrWhiteSpace(req.LastPaymentOperator)
                && !string.IsNullOrWhiteSpace(req.LastPaymentDate)
                && IsValidOperator(req.LastPaymentOperator))
            {
                if (DateTime.TryParse(req.LastPaymentDate, out DateTime parsedDate))
                {
                    sb.Append($" AND lst_pmt_date {req.LastPaymentOperator} ?");
                    parameters.Add(parsedDate.ToString("yyyy-MM-dd")); // ✅ formatted
                    logger.Info($"Filter: lst_pmt_date {req.LastPaymentOperator} {parsedDate:yyyy-MM-dd}");
                }
                else
                {
                    logger.Warn("Invalid LastPaymentDate format");
                }
            }

            if (req.UseArrearsPosition
                && !string.IsNullOrWhiteSpace(req.ArrearsOperator)
                && !string.IsNullOrWhiteSpace(req.ArrearsPosition)
                && IsValidOperator(req.ArrearsOperator)
                && int.TryParse(req.ArrearsPosition, out _))
            {
                sb.Append($" AND arrears_pos {req.ArrearsOperator} ?");
                parameters.Add(int.Parse(req.ArrearsPosition));
                logger.Info($"Filter: arrears_pos {req.ArrearsOperator} {req.ArrearsPosition}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Returns the distinct values for a single column — used for filter dropdowns.
        /// </summary>
        private List<FilterOptionModel> GetDistinctValues(
            OleDbConnection conn, string areaCode, string billCycle, string columnName)
        {
            var options = new List<FilterOptionModel>();

            try
            {
                string sql = $"SELECT DISTINCT {columnName} FROM prn_dat_1 " +
                             $"WHERE area_code = ? AND bill_cycle = ? " +
                             $"ORDER BY {columnName}";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@area_code", areaCode);
                    cmd.Parameters.AddWithValue("@bill_cycle", billCycle);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var value = reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString().Trim();
                            if (!string.IsNullOrEmpty(value))
                                options.Add(new FilterOptionModel { Value = value, Label = value });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Error loading distinct values for column: {columnName}");
            }

            return options;
        }

        /// <summary>Maps a data reader row to a ListingOfCustomerModel.</summary>
        private ListingOfCustomerModel MapReaderToModel(OleDbDataReader reader)
        {
            var model = new ListingOfCustomerModel();

            try
            {
                model.AccountNumber = GetString(reader, "acct_number");

                // Combine three meter number columns (trim blanks; skip empty segments)
                var m1 = GetString(reader, "met_no1");
                var m2 = GetString(reader, "met_no2");
                var m3 = GetString(reader, "met_no3");
                model.MeterNumbers = CombineNonEmpty(", ", m1, m2, m3);

                // Full name
                var fname = GetString(reader, "cust_fname");
                var lname = GetString(reader, "cust_lname");
                model.CustomerName = $"{fname},{lname}".Trim(',').Trim();

                // Full address
                var a1 = GetString(reader, "address_1");
                var a2 = GetString(reader, "address_2");
                var a3 = GetString(reader, "address_3");
                model.Address = CombineNonEmpty(", ", a1, a2, a3);

                model.Tariff = GetString(reader, "tariff_code");
                model.CurrentDepot = GetString(reader, "crnt_depot");
                model.Transformer = GetString(reader, "substn_code");
                model.ReaderCode = GetString(reader, "reader_code");

                model.RawKwhCharge = GetDecimal(reader, "kwh_charge");
                model.RawCurrentBalance = GetDecimal(reader, "crnt_balance");
                model.KwhCharge = model.RawKwhCharge.ToString("###,##0.##");
                model.CurrentBalance = model.RawCurrentBalance.ToString("###,##0.##");

                model.NoOfPhase = GetString(reader, "no_of_phase");
                model.ConnectionType = GetString(reader, "conect_type");
                model.DailyPackNo = GetString(reader, "dly_pack_no");
                model.WalkSeq = GetString(reader, "walk_seq");
                model.KvaRating = GetString(reader, "kva_rating");

                model.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping reader to ListingOfCustomerModel");
                model.ErrorMessage = ex.Message;
            }

            return model;
        }

        /// <summary>Looks up the area name for a given area code.</summary>
        private string GetAreaName(OleDbConnection conn, string areaCode)
        {
            try
            {
                const string sql = "SELECT area_name FROM areas WHERE area_code = ?";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@area_code", areaCode);
                    var result = cmd.ExecuteScalar();
                    return (result != null && result != DBNull.Value)
                        ? result.ToString().Trim()
                        : string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Error getting area name for code: {areaCode}");
                return string.Empty;
            }
        }

        // ── Low-level helpers ────────────────────────────────────────────────

        private string GetString(OleDbDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                return reader.IsDBNull(ordinal) ? "" : reader.GetValue(ordinal).ToString().Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return "";
            }
        }

        private decimal GetDecimal(OleDbDataReader reader, string column)
        {
            try
            {
                var ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return 0;
                return Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return 0;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Invalid decimal in column '{column}'");
                return 0;
            }
        }

        /// <summary>Joins non-empty segments with a separator.</summary>
        private static string CombineNonEmpty(string separator, params string[] parts)
        {
            var nonEmpty = new List<string>();
            foreach (var p in parts)
                if (!string.IsNullOrWhiteSpace(p))
                    nonEmpty.Add(p);
            return string.Join(separator, nonEmpty);
        }

        /// <summary>
        /// Whitelist check for comparison operators — prevents operator injection.
        /// </summary>
        private static bool IsValidOperator(string op)
        {
            switch (op?.Trim())
            {
                case "=":
                case ">":
                case "<":
                case ">=":
                case "<=":
                    return true;
                default:
                    return false;
            }
        }
    }
}