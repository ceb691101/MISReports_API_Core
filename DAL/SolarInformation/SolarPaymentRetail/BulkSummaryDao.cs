using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.SolarInformation.SolarPaymentRetail
{
    public class BulkSummaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // Use bulk connection
        }

        public List<RetailSummaryModel> GetRetailBulkSummaryReport(RetailSummaryRequest request)
        {
            var results = new List<RetailSummaryModel>();

            try
            {
                logger.Info("=== START GetRetailBulkSummaryReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}");

                using (var conn = _dbConnection.GetConnection(true)) // Use bulk connection
                {
                    conn.Open();

                    // Step 1: Get main summary data from netmtcons
                    string sql = BuildBulkSummaryQuery();
                    logger.Debug($"Bulk summary query SQL: {sql}");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300; // 5 minutes
                        cmd.Parameters.AddWithValue("@billCycle", request.BillCycle);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var netTypeCode = GetStringValue(reader, "net_type");

                                var model = new RetailSummaryModel
                                {
                                    NetType = netTypeCode, // Will be mapped later
                                    NoOfAccounts = GetIntValue(reader, "account_count"),
                                    EnergyImported = GetIntValue(reader, "total_units_in"),
                                    EnergyExported = GetIntValue(reader, "total_units_out"),
                                    UnitSaleKwh = GetIntValue(reader, "total_unit_sale"),
                                    UnitSaleRs = GetDecimalValue(reader, "total_kwh_sales"),
                                    KwhPayableBalance = 0, // Will be calculated based on net type
                                    ErrorMessage = string.Empty
                                };

                                // Step 2: Calculate payable balance based on net type
                                model.KwhPayableBalance = CalculatePayableBalance(conn, request.BillCycle, netTypeCode);

                                // Step 3: Map net type code to friendly name
                                model.NetType = MapNetTypeToCustomerType(netTypeCode);

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetRetailBulkSummaryReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching retail bulk summary report");
                throw;
            }
        }

        private string BuildBulkSummaryQuery()
        {
            // Note: Using imp_kwo_units + imp_kwd_units + imp_kwp_units for imported
            // and exp_kwo_units + exp_kwd_units + exp_kwp_units for exported
            string sql = @"SELECT 
                            COUNT(acc_nbr) as account_count,
                            SUM(imp_kwo_units + imp_kwd_units + imp_kwp_units) as total_units_in,
                            SUM(exp_kwo_units + exp_kwd_units + exp_kwp_units) as total_units_out,
                            SUM(gen_cap) as total_gen_cap,
                            net_type,
                            SUM(kwh_sales) as total_kwh_sales,
                            SUM(unitsale) as total_unit_sale,
                            SUM(kwh_sales - bill_settle) as calculated_balance
                            FROM netmtcons 
                            WHERE bill_cycle = ? 
                            GROUP BY net_type 
                            ORDER BY net_type";

            return sql;
        }

        private decimal CalculatePayableBalance(OleDbConnection conn, string billCycle, string netType)
        {
            try
            {
                // Net Metering (type 1) has no payable balance
                if (netType == "1")
                {
                    logger.Debug("Net type 1 (Net Metering) - payable balance is 0");
                    return 0;
                }

                // For other net types, query the transac table
                string txnType = GetTransactionType(netType);

                if (string.IsNullOrEmpty(txnType))
                {
                    logger.Warn($"Unknown net type for transaction lookup: {netType}");
                    return 0;
                }

                string sql = @"SELECT SUM(TXN_AMT) as total_amount 
                              FROM transac 
                              WHERE added_blcy = ? 
                              AND txn_type = ?";

                logger.Debug($"Payable balance query for net_type {netType}: {sql}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.CommandTimeout = 300;
                    cmd.Parameters.AddWithValue("@billCycle", billCycle);
                    cmd.Parameters.AddWithValue("@txnType", txnType);

                    var result = cmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        logger.Debug($"No transaction amount found for net_type {netType}, bill_cycle {billCycle}");
                        return 0;
                    }

                    decimal payableBalance = Convert.ToDecimal(result);
                    logger.Debug($"Payable balance for net_type {netType}: {payableBalance}");
                    return payableBalance;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error calculating payable balance for net_type {netType}");
                return 0;
            }
        }

        private string GetTransactionType(string netType)
        {
            // Map net type to transaction type
            switch (netType)
            {
                case "2":
                    return "MA"; // Net Accounting
                case "3":
                    return "MP"; // Net Plus
                case "4":
                    return "ML"; // Net Plus Plus
                default:
                    return null;
            }
        }

        private string MapNetTypeToCustomerType(string netType)
        {
            if (string.IsNullOrEmpty(netType))
                return "Unknown";

            switch (netType.Trim())
            {
                case "1":
                    return "Net Metering";
                case "2":
                    return "Net Accounting";
                case "3":
                    return "Net Plus";
                case "4":
                    return "Net Plus Plus";
                case "5":
                    return "Convert Net Metering to Net Accounting";
                default:
                    return "Unknown";
            }
        }

        // Helper methods
        private string GetStringValue(OleDbDataReader reader, string columnName)
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