using MISReports_Api.Compat;
using MISReports_Api.Models.CustomerDetails;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MISReports_Api.DAL.CustomerDetails
{
    public class StandingOrderDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string ConnectionName = "InformixStandingOrder";

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "TestConnection failed");
                errorMessage = ex.Message;
                return false;
            }
        }

        private static DbConnection GetConnection()
        {
            var settings = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException($"Connection string \'{ConnectionName}\' is missing from connectionStrings.config.");
            }
            return Informix.CreateConnection(settings.ConnectionString);
        }

        public StandingOrderResponse GetStandingOrderDetails(string acctNumber)
        {
            var response = new StandingOrderResponse
            {
                AccountNumber = acctNumber,
                Records = new List<StandingOrderBillRecord>(),
                IsRegistered = false,
                ErrorMessage = null
            };

            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Step 1: Check customer availability in stod_cust
                    const string custSql = "SELECT * FROM stod_cust WHERE acct_number = ?";
                    using (var cmd = Informix.CreateCommand(custSql, conn))
                    {
                        cmd.Parameters.Add("?", OleDbType.VarChar).Value = acctNumber.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                response.IsRegistered = false;
                                response.ErrorMessage = "Customer not registered for this service";
                                return response;
                            }

                            string status1 = GetString(reader, "status1").ToUpperInvariant();
                            if (status1 != "A" && status1 != "S")
                            {
                                response.IsRegistered = false;
                                response.Status = status1;
                                response.ErrorMessage = "Customer not registered for this service";
                                return response;
                            }

                            response.IsRegistered = true;
                            response.Status = status1 == "A" ? "Active" : (status1 == "S" ? "Inactive" : status1);
                        }
                    }

                    // Step 2: Fetch bill settlement details and customer info from mnth_bill
                    const string billSql = "SELECT * FROM mnth_bill WHERE acct_number = ? ORDER BY bill_cycle DESC";
                    using (var cmd = Informix.CreateCommand(billSql, conn))
                    {
                        cmd.Parameters.Add("?", OleDbType.VarChar).Value = acctNumber.Trim();

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (string.IsNullOrWhiteSpace(response.Name))
                                {
                                    string fname = GetString(reader, "cust_fname");
                                    string lname = GetString(reader, "cust_lname");
                                    response.Name = $"{fname} {lname}".Trim();

                                    string a1 = GetString(reader, "address_1");
                                    string a2 = GetString(reader, "address_2");
                                    string a3 = GetString(reader, "address_3");
                                    response.Address = $"{a1} {a2} {a3}".Trim();
                                }

                                string reqstStat = GetString(reader, "reqst_stat");
                                if (string.Equals(reqstStat, "W", StringComparison.OrdinalIgnoreCase))
                                {
                                    reqstStat = "Waiting";
                                }
                                else if (string.Equals(reqstStat, "U", StringComparison.OrdinalIgnoreCase))
                                {
                                    reqstStat = "fetched";
                                }

                                string procDate = GetDateString(reader, "proc_date");
                                string procTime = GetString(reader, "proc_time");
                                string fullProcTime = $"{procDate} {procTime}".Trim();

                                response.Records.Add(new StandingOrderBillRecord
                                {
                                    BillMonth = GetString(reader, "bill_mon"),
                                    FromDate = GetDateString(reader, "frm_date"),
                                    ToDate = GetDateString(reader, "to_date"),
                                    KwhUnits = GetDouble(reader, "kwh_units"),
                                    KwhCharge = GetDecimal(reader, "kwh_charge"),
                                    Payments = GetDecimal(reader, "payments"),
                                    OpeningBalance = GetDecimal(reader, "open_bal"),
                                    ClosingBalance = GetDecimal(reader, "close_bal"),
                                    BillProcessTime = fullProcTime,
                                    RequestTimeBank = GetString(reader, "reqst_time"),
                                    RequestStatus = reqstStat
                                });
                            }
                        }
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error fetching standing order details for account {acctNumber}");
                response.ErrorMessage = $"Error loading data from database: {ex.Message}";
                return response;
            }
        }

        private static string GetString(DbDataReader reader, string column)
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return string.Empty;
                return reader.GetValue(ordinal)?.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetDateString(DbDataReader reader, string column)
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return string.Empty;
                var val = reader.GetValue(ordinal);
                if (val is DateTime dt) return dt.ToString("dd/MM/yyyy");
                if (DateTime.TryParse(val?.ToString(), out var parsed)) return parsed.ToString("dd/MM/yyyy");
                return val?.ToString()?.Trim() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static double GetDouble(DbDataReader reader, string column)
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return 0.0;
                return Convert.ToDouble(reader.GetValue(ordinal));
            }
            catch
            {
                return 0.0;
            }
        }

        private static decimal GetDecimal(DbDataReader reader, string column)
        {
            try
            {
                int ordinal = reader.GetOrdinal(column);
                if (reader.IsDBNull(ordinal)) return 0m;
                return Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch
            {
                return 0m;
            }
        }
    }
}

