using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;

namespace MISReports_Api.DAL.Dashboard
{
    public class SalesAndCollectionRangeDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string _posPaymentConnectionString;
        private readonly string _bankPaymentConnectionString;

        public SalesAndCollectionRangeDao()
        {
            _posPaymentConnectionString = GetRequiredConnectionString("InformixPosPayment");
            _bankPaymentConnectionString = GetRequiredConnectionString("InformixBankPayment");
        }

        // ------------------------------------------------------------------ //
        // Connection test (Ordinary DB — both queries hit the ordinary DB)
        // ------------------------------------------------------------------ //
        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, useBulkConnection: false);
        }

        // ------------------------------------------------------------------ //
        // Public entry point
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Fetches daily sales and collection totals for the last 7 days
        /// (from day-before-today back to 6 days earlier).
        /// Ordinary (bill_type='O') and bulk (bill_type='B') are queried separately.
        /// </summary>
        public SalesAndCollectionRangeResult GetSalesAndCollectionRange(string region = null, string province = null, string area = null)
        {
            var result = new SalesAndCollectionRangeResult();

            try
            {
                logger.Info("=== START GetSalesAndCollectionRange ===");

                // Match financial dashboard logic: include [today-7 .. today-1].
                DateTime fromDate = DateTime.Today.AddDays(-7);
                DateTime toDate = DateTime.Today.AddDays(-1);

                result.OrdinaryData = GetOrdinarySalesAndCollectionByDateRange(fromDate, toDate, region, province, area);
                logger.Info($"Ordinary records fetched: {result.OrdinaryData.Count}");

                result.BulkData = GetBulkSalesAndCollectionByDateRange(fromDate, toDate, region, province, area);
                logger.Info($"Bulk records fetched: {result.BulkData.Count}");

                logger.Info("=== END GetSalesAndCollectionRange (Success) ===");
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetSalesAndCollectionRange");
                throw;
            }
        }

        // ------------------------------------------------------------------ //
        // Private helpers
        // ------------------------------------------------------------------ //

        private List<SalesAndCollectionModel> GetOrdinarySalesAndCollectionByDateRange(DateTime fromDate, DateTime toDate, string region, string province, string area)
        {
            return GetSalesAndCollectionByDateRange(fromDate, toDate, "O", region, province, area);
        }

        private List<SalesAndCollectionModel> GetBulkSalesAndCollectionByDateRange(DateTime fromDate, DateTime toDate, string region, string province, string area)
        {
            return GetSalesAndCollectionByDateRange(fromDate, toDate, "B", region, province, area);
        }

        private List<SalesAndCollectionModel> GetSalesAndCollectionByDateRange(DateTime fromDate, DateTime toDate, string billType, string region, string province, string area)
        {
            var dailyCollection = new Dictionary<DateTime, decimal>();

            bool hasAreaFilter = !string.IsNullOrWhiteSpace(area);
            bool hasProvinceFilter = !string.IsNullOrWhiteSpace(province);
            bool hasRegionFilter = !string.IsNullOrWhiteSpace(region);
            bool hasFilter = hasAreaFilter || hasProvinceFilter || hasRegionFilter;
            string filterColumn = hasAreaFilter ? "area_code" : (hasProvinceFilter ? "prov_code" : "region");
            string resolvedProv = hasProvinceFilter ? province.Trim().ToUpperInvariant() : null;
            if (hasProvinceFilter && resolvedProv.StartsWith("0") && resolvedProv.Length > 1 && char.IsDigit(resolvedProv[1]))
            {
                resolvedProv = resolvedProv.Substring(1);
            }
            string filterValue = hasAreaFilter ? area.Trim().ToUpperInvariant() : 
                                 (hasProvinceFilter ? resolvedProv : 
                                 (hasRegionFilter ? region.Trim().ToUpperInvariant() : null));

            string posCollectionSql = @"
                SELECT c.trans_date,
                       SUM(c.trans_amt) AS amount
                FROM   cus_tran c, areas a
                WHERE  c.area_code = a.area_code
                  AND  c.bill_type = ?
                  AND  c.trans_type = 0
                  AND  c.trans_date >= ?
                  AND  c.trans_date <= ?
                  " + (hasFilter ? $"AND a.{filterColumn} = ?\n" : string.Empty) + @"
                GROUP BY 1
                ORDER BY 1";

            string bankCashSql = @"
                SELECT b.cash_date,
                       SUM(b.paid_amount) AS amount
                FROM   bank_paymast b, bankname c, areas p
                WHERE  b.area_code = p.area_code
                  AND  b.bank_code = c.bank_code
                  AND  b.cash_date >= ?
                  AND  b.cash_date <= ?
                  AND  b.bill_type = ?
                  " + (hasFilter ? $"AND p.{filterColumn} = ?\n" : string.Empty) + @"
                GROUP BY 1
                ORDER BY 1";

            string cardCashSql = @"
                SELECT b.cash_date,
                       SUM(b.tot_amt) AS amount
                FROM   crdtauth b, areas p
                WHERE  b.area_code = p.area_code
                  AND  b.cash_date >= ?
                  AND  b.cash_date <= ?
                  AND  b.bill_type = ?
                  " + (hasFilter ? $"AND p.{filterColumn} = ?\n" : string.Empty) + @"
                GROUP BY 1
                ORDER BY 1";

            try
            {
                logger.Info($"=== START GetSalesAndCollectionByDateRange billType={billType}: {fromDate:dd-MM-yy} to {toDate:dd-MM-yy} ===");

                using (var posConn = new OleDbConnection(_posPaymentConnectionString))
                {
                    posConn.Open();
                    AddDateAmountRowsFromOleDb(
                        conn: posConn,
                        sql: posCollectionSql,
                        destination: dailyCollection,
                        billType: billType,
                        fromDate: fromDate.Date,
                        toDate: toDate.Date,
                        filterValue: filterValue);
                }

                using (var bankConn = new OleDbConnection(_bankPaymentConnectionString))
                {
                    bankConn.Open();

                    AddDateAmountRowsFromOleDb(
                        conn: bankConn,
                        sql: bankCashSql,
                        destination: dailyCollection,
                        billType: billType,
                        fromDate: fromDate.Date,
                        toDate: toDate.Date,
                        filterValue: filterValue);

                    AddDateAmountRowsFromOleDb(
                        conn: bankConn,
                        sql: cardCashSql,
                        destination: dailyCollection,
                        billType: billType,
                        fromDate: fromDate.Date,
                        toDate: toDate.Date,
                        filterValue: filterValue);
                }

                var rows = new List<SalesAndCollectionModel>();

                for (var day = fromDate.Date; day <= toDate.Date; day = day.AddDays(1))
                {
                    var amount = dailyCollection.TryGetValue(day, out var value) ? value : 0m;

                    rows.Add(new SalesAndCollectionModel
                    {
                        Date = day.ToString("dd-MM-yy", CultureInfo.InvariantCulture),
                        Collection = amount,
                        ErrorMessage = string.Empty
                    });
                }
                logger.Info($"=== END GetSalesAndCollectionByDateRange billType={billType} (rows: {rows.Count}) ===");
                return rows;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error in GetSalesAndCollectionByDateRange for billType='{billType}'");
                throw;
            }
        }

        private void AddDateAmountRowsFromOleDb(
            OleDbConnection conn,
            string sql,
            Dictionary<DateTime, decimal> destination,
            string billType,
            DateTime fromDate,
            DateTime toDate,
            string filterValue)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    if (sql.Contains("trans_type"))
                    {
                        cmd.Parameters.AddWithValue("?", billType);
                        cmd.Parameters.AddWithValue("?", fromDate);
                        cmd.Parameters.AddWithValue("?", toDate);
                        if (!string.IsNullOrWhiteSpace(filterValue))
                            cmd.Parameters.AddWithValue("?", filterValue);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("?", fromDate);
                        cmd.Parameters.AddWithValue("?", toDate);
                        cmd.Parameters.AddWithValue("?", billType);
                        if (!string.IsNullOrWhiteSpace(filterValue))
                            cmd.Parameters.AddWithValue("?", filterValue);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var date = Convert.ToDateTime(reader[0]).Date;
                            var amount = reader[1] == DBNull.Value ? 0m : Convert.ToDecimal(reader[1]);

                            if (destination.ContainsKey(date))
                                destination[date] += amount;
                            else
                                destination[date] = amount;
                        }
                    }
                }
            }
            catch (OleDbException ex) when (IsNoRecordFound(ex.Message))
            {
                logger.Warn($"No records found for query/date range. Source skipped. Details: {ex.Message}");
            }
        }

        private static bool IsNoRecordFound(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            return message.IndexOf("no record found", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetRequiredConnectionString(string key)
        {
            var setting = ConfigurationManager.ConnectionStrings[key];

            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
                throw new ConfigurationErrorsException($"{key} connection string is missing or empty in configuration.");

            return setting.ConnectionString;
        }

        // ------------------------------------------------------------------ //
        // Reader helpers (mirrors the pattern used across the codebase)
        // ------------------------------------------------------------------ //

        private int GetIntValue(OleDbDataReader reader, string column)
        {
            try
            {
                var value = reader[column];
                return value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid int format in column '{column}'");
                return 0;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string column)
        {
            try
            {
                var value = reader[column];
                return value == DBNull.Value ? 0m : Convert.ToDecimal(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return 0m;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column '{column}'");
                return 0m;
            }
        }
    }
}