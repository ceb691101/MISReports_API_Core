using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Dashboard
{
    public class KioskCollectionDao
    {
        private readonly string _connectionString;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public KioskCollectionDao()
        {
            var connection = ConfigurationManager.ConnectionStrings["InformixPosPayment"];

            if (connection == null || string.IsNullOrWhiteSpace(connection.ConnectionString))
            {
                throw new ConfigurationErrorsException("InformixPosPayment connection string is missing or empty in configuration.");
            }

            _connectionString = connection.ConnectionString;
        }

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (OleDbException ex)
            {
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "OLE DB connection failed with no message."
                    : ex.Message;

                errorMessage = $"{message} (HResult: 0x{ex.ErrorCode:X8})";
                logger.Error(ex, "Kiosk POS DB connection test failed (OLE DB).");
                return false;
            }
            catch (Exception ex)
            {
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? "Database connection failed with no message."
                    : ex.Message;

                errorMessage = $"{message} (HResult: 0x{ex.HResult:X8})";
                logger.Error(ex, "Kiosk POS DB connection test failed.");
                return false;
            }
        }

        public List<KioskCollectionModel> GetKioskCollection(string userId, string region = null, string province = null, string area = null)
        {
            var rows = new List<KioskCollectionModel>();

            try
            {
                var toDate = DateTime.Today.AddDays(-1);
                var fromDate = toDate.AddDays(-6);
                // logger.Info($"=== START GetKioskCollection userId={userId}, from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} ===");
                logger.Info($"=== START GetKioskCollection userId={userId}, from {fromDate:dd-MM-yy} to {toDate:dd-MM-yy} ===");

                rows = QueryKioskCollection(userId: userId, region: region, province: province, area: area);

                logger.Info($"=== END GetKioskCollection (Success) - {rows.Count} records ===");
                return rows;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetKioskCollection");
                throw;
            }
        }

        private List<KioskCollectionModel> QueryKioskCollection(string userId, string region, string province, string area)
        {
            var rows = new List<KioskCollectionModel>();

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

            string sql = hasFilter
                    ? $@"
                                                                SELECT DATE(c.trans_date) AS trans_date,
                                             SUM(c.trans_amt) AS collection
                                FROM   cus_tran c, areas a
                                                                WHERE  c.userid = ?
                                                                        AND  c.trans_date >= TODAY - 7
                                                                        AND  c.trans_date <  TODAY
                                    AND  c.bill_type = 'O'
                                    AND  c.area_code = a.area_code
                                    AND  a.{filterColumn} = ?
                                GROUP BY 1
                                ORDER BY 1"
                    : @"
                                                                SELECT DATE(trans_date) AS trans_date,
                                             SUM(trans_amt) AS collection
                                FROM   cus_tran
                                                                WHERE  userid = ?
                                                                        AND  trans_date >= TODAY - 7
                                                                        AND  trans_date <  TODAY
                                    AND  bill_type = 'O'
                                GROUP BY 1
                                ORDER BY 1";

            using (var conn = new OleDbConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", userId);
                    if (hasFilter)
                    {
                        cmd.Parameters.AddWithValue("?", filterValue);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new KioskCollectionModel
                            {
                                TransDate = GetDateStringValue(reader, "trans_date"),
                                CollectionAmount = GetLongValue(reader, "collection"),
                                ErrorMessage = string.Empty
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private string GetDateStringValue(OleDbDataReader reader, string column)
        {
            try
            {
                var value = reader[column];
                if (value == DBNull.Value)
                    return string.Empty;

                // return Convert.ToDateTime(value).ToString("yyyy-MM-dd");
                return Convert.ToDateTime(value).ToString("dd-MM-yy");
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return string.Empty;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid date format in column '{column}'");
                return string.Empty;
            }
        }

        private long GetLongValue(OleDbDataReader reader, string column)
        {
            try
            {
                var value = reader[column];
                return value == DBNull.Value ? 0L : Convert.ToInt64(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{column}' not found in result set");
                return 0L;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid whole number format in column '{column}'");
                return 0L;
            }
        }
    }
}