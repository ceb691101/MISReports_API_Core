using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.PUCSLReports;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.PUCSLReports
{
    public class BulkPUCSLSolarCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // Using bulk connection
        }

        public List<BulkPUCSLSolarCustomersModel> GetBulkPUCSLSolarCustomersReport(BulkPUCSLSolarCustomersRequest request)
        {
            var results = new List<BulkPUCSLSolarCustomersModel>();

            if (request == null)
                throw new ArgumentException("Request cannot be null.");

            if (string.IsNullOrWhiteSpace(request.Region))
                throw new ArgumentException("Region is required.");

            if (string.IsNullOrWhiteSpace(request.FromBillCycle) || string.IsNullOrWhiteSpace(request.ToBillCycle))
                throw new ArgumentException("FromBillCycle and ToBillCycle are required.");

            if (!int.TryParse(request.FromBillCycle, out int fromCycle) ||
                !int.TryParse(request.ToBillCycle, out int toCycle))
                throw new ArgumentException("FromBillCycle and ToBillCycle must be numeric.");

            if (fromCycle > toCycle)
                throw new ArgumentException("FromBillCycle cannot be greater than ToBillCycle.");

            try
            {
                logger.Info("=== START GetBulkPUCSLSolarCustomersReport ===");
                logger.Info($"Request: Region={request.Region}, FromBillCycle={request.FromBillCycle}, ToBillCycle={request.ToBillCycle}");

                const string sql = @"
                    SELECT a.region,
                           n.bill_cycle,
                           (CASE
                                WHEN n.net_type IN ('1') THEN 'Net Metering'
                                WHEN n.net_type IN ('2') THEN 'Net Accounting'
                                WHEN n.net_type IN ('3') THEN 'Net Plus'
                                WHEN n.net_type IN ('4') THEN 'Net Plus Plus'
                                ELSE ''
                           END) AS net_type,
                           n.area_cd,
                           COUNT(n.acc_nbr) AS No_Of_Accounts,
                           COALESCE(SUM(n.unitsale), 0) AS sale,
                           COALESCE(SUM(n.exp_kwd_units), 0) AS export,
                           COALESCE(SUM(n.imp_kwo_units + n.imp_kwd_units + n.imp_kwp_units), 0) AS import,
                           COALESCE(SUM(n.kwh_sales), 0) AS kwh_sales
                    FROM netmtcons n, areas a
                    WHERE TO_NUMBER(n.bill_cycle) >= ?
                      AND TO_NUMBER(n.bill_cycle) <= ?
                      AND n.net_type IN ('1', '2', '3', '4')
                      AND a.area_code = n.area_cd
                      AND a.region = ?
                    GROUP BY a.region, n.bill_cycle, net_type, n.area_cd
                    ORDER BY 1, 2, 3, 4";

                logger.Debug($"Query SQL: {sql}");

                using (var conn = _dbConnection.GetConnection(true)) // Using bulk connection
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300; // 5 minutes

                        // OleDb uses positional (?) parameters — order must match the query.
                        cmd.Parameters.AddWithValue("@fromBillCycle", fromCycle);
                        cmd.Parameters.AddWithValue("@toBillCycle", toCycle);
                        cmd.Parameters.AddWithValue("@region", request.Region);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var billCycle = GetColumnValue(reader, "bill_cycle");

                                var model = new BulkPUCSLSolarCustomersModel
                                {
                                    Region = GetColumnValue(reader, "region"),
                                    BillCycle = billCycle,
                                    Period = TryConvertToMonthYear(billCycle),
                                    NetType = GetColumnValue(reader, "net_type"),
                                    AreaCode = GetColumnValue(reader, "area_cd"),
                                    NoOfAccounts = GetIntValue(reader, "No_Of_Accounts"),
                                    Sale = GetIntValue(reader, "sale"),
                                    Export = GetIntValue(reader, "export"),
                                    Import = GetIntValue(reader, "import"),
                                    KwhSales = GetDecimalValue(reader, "kwh_sales"),
                                    ErrorMessage = string.Empty
                                };

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetBulkPUCSLSolarCustomersReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching bulk PUCSL solar customers report");
                throw;
            }
        }

        private static string TryConvertToMonthYear(string billCycle)
        {
            if (int.TryParse(billCycle, out int cycle))
                return BillCycleHelper.ConvertToMonthYear(cycle);

            return string.Empty;
        }

        // Helper methods
        private string GetColumnValue(OleDbDataReader reader, string columnName)
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

        private int GetIntValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid int format in column '{columnName}'");
                return 0;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column '{columnName}'");
                return 0;
            }
        }
    }
}