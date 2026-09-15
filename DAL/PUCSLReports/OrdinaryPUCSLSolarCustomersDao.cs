using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.PUCSLReports;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.PUCSLReports
{
    public class OrdinaryPUCSLSolarCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false); // Using ordinary connection
        }

        public List<OrdinaryPUCSLSolarCustomersModel> GetOrdinaryPUCSLSolarCustomersReport(OrdinaryPUCSLSolarCustomersRequest request)
        {
            var results = new List<OrdinaryPUCSLSolarCustomersModel>();

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
                logger.Info("=== START GetOrdinaryPUCSLSolarCustomersReport ===");
                logger.Info($"Request: Region={request.Region}, FromBillCycle={request.FromBillCycle}, ToBillCycle={request.ToBillCycle}");

                // NOTE: "Net" is aliased as net_value here (NET is a reserved/ambiguous
                // word in several SQL engines) and mapped back onto the model's Net property.
                const string sql = @"
                    SELECT a.region, n.calc_cycle, n.area_code, 'Net Metering' AS net_type,
                           COUNT(n.acct_number) AS No_Of_Accounts,
                           SUM(n.units_in) AS import,
                           SUM(n.units_out) AS export,
                           0 AS net_value
                    FROM netmtcons n, areas a
                    WHERE n.net_type IN ('1')
                      AND TO_NUMBER(n.calc_cycle) >= ?
                      AND TO_NUMBER(n.calc_cycle) <= ?
                      AND a.area_code = n.area_code
                      AND a.region = ?
                    GROUP BY a.region, n.calc_cycle, n.area_code

                    UNION ALL

                    SELECT a.region, n.calc_cycle, n.area_code, 'Net Accounting' AS net_type,
                           COUNT(n.acct_number) AS No_Of_Accounts,
                           SUM(n.units_in) AS import,
                           SUM(n.units_out) AS export,
                           SUM(n.units_out - n.units_in) AS net_value
                    FROM netmtcons n, areas a
                    WHERE n.net_type IN ('2', '5')
                      AND TO_NUMBER(n.calc_cycle) >= ?
                      AND TO_NUMBER(n.calc_cycle) <= ?
                      AND a.area_code = n.area_code
                      AND a.region = ?
                    GROUP BY a.region, n.calc_cycle, n.area_code

                    UNION ALL

                    SELECT a.region, n.calc_cycle, n.area_code, 'Net Plus' AS net_type,
                           COUNT(n.acct_number) AS No_Of_Accounts,
                           SUM(n.units_in) AS import,
                           SUM(n.unitsale) AS export,
                           SUM(n.units_out - n.units_in) AS net_value
                    FROM netmtcons n, areas a
                    WHERE n.net_type IN ('3')
                      AND TO_NUMBER(n.calc_cycle) >= ?
                      AND TO_NUMBER(n.calc_cycle) <= ?
                      AND a.area_code = n.area_code
                      AND a.region = ?
                    GROUP BY a.region, n.calc_cycle, n.area_code

                    UNION ALL

                    SELECT a.region, n.calc_cycle, n.area_code, 'Net Plus Plus' AS net_type,
                           COUNT(n.acct_number) AS No_Of_Accounts,
                           SUM(n.units_in) AS import,
                           SUM(n.unitsale) AS export,
                           SUM(n.units_out - n.units_in) AS net_value
                    FROM netmtcons n, areas a
                    WHERE n.net_type IN ('4')
                      AND TO_NUMBER(n.calc_cycle) >= ?
                      AND TO_NUMBER(n.calc_cycle) <= ?
                      AND a.area_code = n.area_code
                      AND a.region = ?

                    GROUP BY a.region, n.calc_cycle, n.area_code
                    ORDER BY 1, 2, 3, 4";

                logger.Debug($"Query SQL: {sql}");

                using (var conn = _dbConnection.GetConnection(false)) // Using ordinary connection
                {
                    conn.Open();

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300; // 5 minutes

                        // OleDb uses positional (?) parameters — order must match the query.
                        // Same 3 params (fromCycle, toCycle, region) repeated once per
                        // UNION ALL branch, in the order each branch appears.
                        for (int i = 0; i < 4; i++)
                        {
                            cmd.Parameters.AddWithValue($"@fromBillCycle{i}", fromCycle);
                            cmd.Parameters.AddWithValue($"@toBillCycle{i}", toCycle);
                            cmd.Parameters.AddWithValue($"@region{i}", request.Region);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var calcCycle = GetColumnValue(reader, "calc_cycle");

                                var model = new OrdinaryPUCSLSolarCustomersModel
                                {
                                    Region = GetColumnValue(reader, "region"),
                                    CalcCycle = calcCycle,
                                    Period = TryConvertToMonthYear(calcCycle),
                                    AreaCode = GetColumnValue(reader, "area_code"),
                                    NetType = GetColumnValue(reader, "net_type"),
                                    NoOfAccounts = GetIntValue(reader, "No_Of_Accounts"),
                                    Import = GetIntValue(reader, "import"),
                                    Export = GetIntValue(reader, "export"),
                                    Net = GetIntValue(reader, "net_value"),
                                    ErrorMessage = string.Empty
                                };

                                results.Add(model);
                            }
                        }
                    }
                }

                logger.Info($"=== END GetOrdinaryPUCSLSolarCustomersReport (Success) - {results.Count} records ===");
                return results;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching ordinary PUCSL solar customers report");
                throw;
            }
        }

        private static string TryConvertToMonthYear(string calcCycle)
        {
            if (int.TryParse(calcCycle, out int cycle))
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
    }
}