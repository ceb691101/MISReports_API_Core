using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.SolarInformation.SolarPaymentRetail
{
    public class OrdSummaryDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false);
        }

        public List<RetailSummaryModel> GetRetailSummaryReport(RetailSummaryRequest request)
        {
            var results = new List<RetailSummaryModel>();

            try
            {
                logger.Info("=== START GetRetailSummaryReport ===");
                logger.Info($"Request: {request.CycleType}Cycle={request.BillCycle ?? request.CalcCycle}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string sql = BuildSummaryQuery(request);
                    logger.Debug($"Summary query SQL: {sql}");

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300; // 5 minutes

                        string cycleValue = request.CycleType == "A" ? request.BillCycle : request.CalcCycle;
                        cmd.Parameters.AddWithValue("@cycle", cycleValue);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var netTypeCode = GetIntValue(reader, "net_type");

                                var model = new RetailSummaryModel
                                {
                                    NetType = MapNetTypeToCustomerType(netTypeCode.ToString()),
                                    NoOfAccounts = GetIntValue(reader, "account_count"),
                                    EnergyExported = GetIntValue(reader, "total_units_out"),
                                    EnergyImported = GetIntValue(reader, "total_units_in"),
                                    UnitSaleKwh = GetIntValue(reader, "total_unit_sale"),
                                    UnitSaleRs = GetDecimalValue(reader, "total_kwh_sales"),
                                    KwhPayableBalance = GetDecimalValue(reader, "payable_balance"),
                                    ErrorMessage = string.Empty
                                };

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetRetailSummaryReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching retail summary report");
                throw;
            }
        }

        private string BuildSummaryQuery(RetailSummaryRequest request)
        {
            string cycleField = request.CycleType == "A" ? "bill_cycle" : "calc_cycle";

            string sql = $@"SELECT 
                            COUNT(acct_number) as account_count,
                            net_type,
                            SUM(units_out) as total_units_out,
                            SUM(units_in) as total_units_in,
                            SUM(gen_cap) as total_gen_cap,
                            SUM(kwh_sales) as total_kwh_sales,
                            SUM(unitsale) as total_unit_sale,
                            SUM(kwh_sales - bill_setle) as payable_balance
                            FROM netmtcons 
                            WHERE {cycleField} = ? 
                            GROUP BY net_type 
                            ORDER BY net_type";

            return sql;
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



