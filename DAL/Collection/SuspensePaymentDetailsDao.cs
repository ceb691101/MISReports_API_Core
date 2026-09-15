using MISReports_Api.DBAccess;
using MISReports_Api.Models.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.Collection
{
    public class SuspensePaymentDetailsDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            // Both reports need both connections (Ordinary for area/province lookups,
            // Bulk for the bulk suspense rows), so validate both up front.
            return _dbConnection.TestAllConnections(out errorMessage);
        }

        /// <summary>
        /// Ordinary Suspense Payment Details (Billsmry) - suspense joined to areas/prov_servers directly.
        /// </summary>
        public List<SuspensePaymentDetailsModel> GetOrdinaryReport(SuspensePaymentDetailsRequest request)
        {
            var results = new List<SuspensePaymentDetailsModel>();

            try
            {
                logger.Info("=== START GetOrdinaryReport (Suspense Payment Details) ===");
                logger.Info($"FromDate={request.FromDate}, ToDate={request.ToDate}");

                using (var conn = _dbConnection.GetConnection(false)) // Ordinary - Billsmry
                {
                    conn.Open();

                    // Explicit column list + table-qualified aliases (instead of SELECT *) to avoid
                    // Informix ambiguous-column issues, since area_code/prov_code appear in more than
                    // one joined table. Ordinal-based reading below is safe as a result.
                    string sql = @"SELECT p.prov_name, s.area_code, a.area_name, s.acct_number, s.bill_cycle,
                                          s.crdt_code, s.susp_amount, s.transac_date, s.pmnt_date, s.post_date,
                                          s.stub_no, s.counter_no
                                   FROM suspense s, areas a, prov_servers p
                                   WHERE s.trnsac_code = 'R0'
                                     AND s.transac_date >= ?
                                     AND s.transac_date <= ?
                                     AND s.area_code = a.area_code
                                     AND p.prov_code = a.prov_code
                                   ORDER BY p.prov_name, a.area_name, s.transac_date";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        AddDateParameter(cmd, request.FromDate);
                        AddDateParameter(cmd, request.ToDate);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string areaCode = GetColumnValue(reader, 1);
                                string areaName = GetColumnValue(reader, 2);

                                var model = new SuspensePaymentDetailsModel
                                {
                                    Province = GetColumnValue(reader, 0),
                                    AreaCode = areaCode,
                                    AreaName = string.IsNullOrEmpty(areaCode) ? areaName : $"{areaCode} - {areaName}",
                                    AccountNumber = GetColumnValue(reader, 3),
                                    BillCycle = GetColumnValue(reader, 4),
                                    CreditCode = GetColumnValue(reader, 5),
                                    SuspenseAmount = GetDecimalValue(reader, 6),
                                    TransacDate = FormatDate(GetColumnValue(reader, 7)),
                                    PaymentDate = FormatDate(GetColumnValue(reader, 8)),
                                    PostDate = FormatDate(GetColumnValue(reader, 9)),
                                    StubNo = GetColumnValue(reader, 10),
                                    CounterNo = GetColumnValue(reader, 11),
                                    ErrorMessage = string.Empty
                                };

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetOrdinaryReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching ordinary suspense payment details");
                throw;
            }
        }

        /// <summary>
        /// Bulk Suspense Payment Details (Billhsbhq) - suspense rows only carry acc_nbr, so
        /// area/province are resolved via offline_payments + prov_servers on the Ordinary side.
        ///
        /// ASSUMPTION: offline_payments and prov_servers are reached through the Ordinary
        /// (Billsmry) connection, since prov_servers is already used that way for the Ordinary
        /// report and DBConnection only exposes Ordinary/Bulk. If area/province come back empty
        /// for bulk rows, this likely needs to point at a separate "PayConsld" connection instead
        /// - let me know and I'll wire up a third connection string.
        /// </summary>
        public List<SuspensePaymentDetailsModel> GetBulkReport(SuspensePaymentDetailsRequest request)
        {
            var results = new List<SuspensePaymentDetailsModel>();

            try
            {
                logger.Info("=== START GetBulkReport (Suspense Payment Details) ===");
                logger.Info($"FromDate={request.FromDate}, ToDate={request.ToDate}");

                List<BulkSuspenseData> bulkRecords;

                using (var conn = _dbConnection.GetConnection(true)) // Bulk - Billhsbhq
                {
                    conn.Open();
                    bulkRecords = GetBulkSuspenseData(conn, request);
                }

                logger.Info($"Retrieved {bulkRecords.Count} bulk suspense records");

                if (bulkRecords.Count == 0)
                {
                    logger.Info("No bulk suspense data found");
                    return results;
                }

                var accountNumbers = bulkRecords.Select(b => b.AccountNumber).Distinct().ToList();

                Dictionary<string, AccountAreaInfo> areaInfoByAccount;
                using (var conn = GetPmntConsldConnection()) // Payment consolidation for offline_payments
                {
                    conn.Open();
                    areaInfoByAccount = GetAccountAreaInfoBatch(conn, accountNumbers);
                }
                logger.Info($"Retrieved area info for {areaInfoByAccount.Count} accounts");

                var provCodes = areaInfoByAccount.Values
                    .Select(v => v.ProvCode)
                    .Where(pc => !string.IsNullOrEmpty(pc))
                    .Distinct()
                    .ToList();

                Dictionary<string, string> provinceNameByCode;
                using (var conn = _dbConnection.GetConnection(false)) // Ordinary for prov_servers lookup
                {
                    conn.Open();
                    provinceNameByCode = GetProvinceNamesBatch(conn, provCodes);
                }
                logger.Info($"Retrieved {provinceNameByCode.Count} province names");

                foreach (var record in bulkRecords)
                {
                    var model = new SuspensePaymentDetailsModel
                    {

                        AccountNumber = record.AccountNumber,
                        BillCycle = record.BillCycle,
                        CreditCode = string.Empty,     // Not available in bulk source
                        SuspenseAmount = record.SuspenseAmount,
                        TransacDate = FormatDate(record.TransacDate),
                        PaymentDate = FormatDate(record.PaymentDate),
                        PostDate = string.Empty,       // Not available in bulk source
                        StubNo = record.StubNo,
                        CounterNo = record.CounterNo,
                        ErrorMessage = string.Empty
                    };

                    if (areaInfoByAccount.TryGetValue(record.AccountNumber, out var areaInfo))
                    {
                        model.AreaCode = areaInfo.AreaCode;
                        model.AreaName = string.IsNullOrEmpty(areaInfo.AreaCode)
                            ? areaInfo.AreaName
                            : $"{areaInfo.AreaCode} - {areaInfo.AreaName}";

                        if (!string.IsNullOrEmpty(areaInfo.ProvCode) &&
                            provinceNameByCode.TryGetValue(areaInfo.ProvCode, out var provName))
                        {
                            model.Province = provName;
                        }
                    }

                    results.Add(model);
                }

                logger.Info($"=== END GetBulkReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching bulk suspense payment details");
                throw;
            }
        }

        /// <summary>
        /// Main bulk suspense rows, ordered per the given SQL (agent_code, cent_code, actl_pay_date).
        /// </summary>
        private List<BulkSuspenseData> GetBulkSuspenseData(OleDbConnection conn, SuspensePaymentDetailsRequest request)
        {
            var results = new List<BulkSuspenseData>();

            string sql = @"SELECT agent_code, cent_code, acc_nbr, post_blcy, paid_amt, actl_pay_date,
                                  credit_date, stub_no, counter
                           FROM suspense
                           WHERE actl_pay_date >= ? AND actl_pay_date <= ?
                           ORDER BY agent_code, cent_code, actl_pay_date";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                AddDateParameter(cmd, request.FromDate);
                AddDateParameter(cmd, request.ToDate);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var data = new BulkSuspenseData
                        {
                            AccountNumber = GetColumnValue(reader, 2),
                            BillCycle = GetColumnValue(reader, 3),
                            SuspenseAmount = GetDecimalValue(reader, 4),
                            TransacDate = GetColumnValue(reader, 5),
                            PaymentDate = GetColumnValue(reader, 6),
                            StubNo = GetColumnValue(reader, 7),
                            CounterNo = GetColumnValue(reader, 8)
                        };

                        results.Add(data);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Batch lookup of area_code/area_name/prov_code per account via offline_payments + areas.
        /// </summary>
        private Dictionary<string, AccountAreaInfo> GetAccountAreaInfoBatch(OleDbConnection conn, List<string> accountNumbers)
        {
            var result = new Dictionary<string, AccountAreaInfo>();

            if (accountNumbers == null || accountNumbers.Count == 0)
                return result;

            const int batchSize = 500;
            for (int i = 0; i < accountNumbers.Count; i += batchSize)
            {
                var batch = accountNumbers.Skip(i).Take(batchSize).ToList();
                var placeholders = string.Join(",", batch.Select(_ => "?"));

                string sql = $@"SELECT f.acc_no, a.area_code, a.area_name, a.prov_code
                               FROM offline_payments f, areas a
                               WHERE a.area_code = f.area_code
                                 AND f.acc_no IN ({placeholders})";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    foreach (var acc in batch)
                        cmd.Parameters.AddWithValue("?", acc);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var accNo = GetColumnValue(reader, 0)?.Trim();
                            if (string.IsNullOrEmpty(accNo))
                                continue;

                            result[accNo] = new AccountAreaInfo
                            {
                                AreaCode = GetColumnValue(reader, 1)?.Trim(),
                                AreaName = GetColumnValue(reader, 2)?.Trim(),
                                ProvCode = GetColumnValue(reader, 3)?.Trim()
                            };
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Batch lookup of prov_name per prov_code via prov_servers.
        /// </summary>
        private Dictionary<string, string> GetProvinceNamesBatch(OleDbConnection conn, List<string> provCodes)
        {
            var result = new Dictionary<string, string>();

            if (provCodes == null || provCodes.Count == 0)
                return result;

            var placeholders = string.Join(",", provCodes.Select(_ => "?"));

            string sql = $@"SELECT TRIM(prov_code), TRIM(prov_name) FROM prov_servers WHERE prov_code IN ({placeholders})";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                foreach (var pc in provCodes)
                    cmd.Parameters.AddWithValue("?", pc?.Trim());

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = GetColumnValue(reader, 0)?.Trim();
                        if (string.IsNullOrEmpty(code))
                            continue;

                        result[code] = GetColumnValue(reader, 1)?.Trim();
                    }
                }
            }

            return result;
        }

        private OleDbConnection GetPmntConsldConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["InformixPmntConsld"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ConfigurationErrorsException("InformixPmntConsld connection string is missing from configuration");

            return new OleDbConnection(connectionString);
        }

        // Helper methods - ordinal-based to stay safe with joined/duplicate column names
        private string GetColumnValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                var value = reader[ordinal];
                return value == DBNull.Value ? null : value.ToString()?.Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column ordinal '{ordinal}' not found in result set");
                return null;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, int ordinal)
        {
            try
            {
                var value = reader[ordinal];
                return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column ordinal '{ordinal}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format at ordinal '{ordinal}'");
                return 0;
            }
        }

        private string FormatDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return "";

            if (DateTime.TryParse(dateStr, out var date))
                return date.ToString("dd-MM-yyyy");

            return dateStr;
        }

        private void AddDateParameter(OleDbCommand cmd, string dateStr)
        {
            if (DateTime.TryParse(dateStr, out var dt))
            {
                cmd.Parameters.AddWithValue("?", dt);
            }
            else
            {
                // If parsing fails, pass the raw string — the DB may accept the literal.
                cmd.Parameters.AddWithValue("?", dateStr ?? string.Empty);
            }
        }

        // Helper classes for batch processing
        private class BulkSuspenseData
        {
            public string AccountNumber { get; set; }
            public string BillCycle { get; set; }
            public decimal SuspenseAmount { get; set; }
            public string TransacDate { get; set; }
            public string PaymentDate { get; set; }
            public string StubNo { get; set; }
            public string CounterNo { get; set; }
        }

        private class AccountAreaInfo
        {
            public string AreaCode { get; set; }
            public string AreaName { get; set; }
            public string ProvCode { get; set; }
        }
    }
}