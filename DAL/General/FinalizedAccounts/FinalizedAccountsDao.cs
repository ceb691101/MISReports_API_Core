using MISReports_Api.DBAccess;
using MISReports_Api.Models.Collection;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Collection
{
    public class FinalizedAccountsDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly DBConnection _dbConnection = new DBConnection();

        public FinalizedAccountsDropdowns GetDropdowns(string provCode = null)
        {
            var dropdowns = new FinalizedAccountsDropdowns
            {
                Provinces = new List<ProvinceOption>(),
                Areas = new List<AreaOption>(),
                BillCycles = new List<string>(),
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    if (string.IsNullOrEmpty(provCode))
                    {
                        using (var cmd = new OleDbCommand("Select * from provinces where prov_code not in('0','Z') order by prov_name", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                dropdowns.Provinces.Add(new ProvinceOption
                                {
                                    ProvCode = GetStringValue(reader, "prov_code"),
                                    ProvName = GetStringValue(reader, "prov_name")
                                });
                            }
                        }

                        using (var cmd = new OleDbCommand("Select * from yr_mnth where bill_cycle>=233 order by bill_cycle desc", conn))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var val = GetStringValue(reader, "bill_cycle");
                                if (!string.IsNullOrEmpty(val)) dropdowns.BillCycles.Add(val);
                            }
                        }
                    }
                    else
                    {
                        using (var cmd = new OleDbCommand("Select * from areas where prov_code=? order by area_name", conn))
                        {
                            cmd.Parameters.AddWithValue("?", provCode);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    dropdowns.Areas.Add(new AreaOption
                                    {
                                        AreaCode = GetStringValue(reader, "area_code"),
                                        AreaName = GetStringValue(reader, "area_name")
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching dropdowns for Finalized Accounts");
                dropdowns.ErrorMessage = ex.Message;
            }

            return dropdowns;
        }

        public FinalizedAccountsResponse GetReport(FinalizedAccountsRequest request)
        {
            var response = new FinalizedAccountsResponse
            {
                Records = new List<FinalizedAccountsRecord>(),
                ErrorMessage = string.Empty
            };

            try
            {
                if (string.IsNullOrEmpty(request.ProvinceCode))
                {
                    throw new Exception("Province code is required.");
                }

                string serverName = "";
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("select * from prov_servers where prov_code=?", conn))
                    {
                        cmd.Parameters.AddWithValue("?", request.ProvinceCode);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                serverName = GetStringValue(reader, "prov_b_svr");
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(serverName))
                {
                    throw new Exception($"Could not find billing server name for province code {request.ProvinceCode}");
                }

                var template = ConfigurationManager.ConnectionStrings["InformixBillingTemplate"]?.ConnectionString;
                if (string.IsNullOrEmpty(template))
                {
                    throw new Exception("InformixBillingTemplate not found in Web.config");
                }

                // Add billing@ prefix if missing, as seen in other DAOs
                string ds = serverName.Trim();
                if (!ds.Contains("@"))
                {
                    ds = $"billing@{ds}";
                }
                else
                {
                    var parts = ds.Split(new[] { '@' }, 2);
                    var dbPart = parts[0];
                    if (dbPart.Trim().ToLowerInvariant().StartsWith("pos"))
                    {
                        ds = "billing@" + parts[1];
                    }
                }

                string connectionString = string.Format(template, ds);

                string sql = "Select * From finalised where 1=1";

                bool isAllAreas = request.AreaCode == "**" || string.IsNullOrEmpty(request.AreaCode);
                bool isAllBillCycles = request.BillCycle == "***" || request.BillCycle == "*** - All Months" || string.IsNullOrEmpty(request.BillCycle);

                if (!isAllAreas)
                {
                    sql += $" and area_code='{request.AreaCode}'";
                }

                if (!isAllBillCycles)
                {
                    string bc = request.BillCycle.Length >= 3 ? request.BillCycle.Substring(0, 3) : request.BillCycle;
                    sql += $" and bill_cycle='{bc}'";
                }

                if (request.BalanceChecked && !string.IsNullOrEmpty(request.BalanceOperator) && !string.IsNullOrEmpty(request.BalanceValue))
                {
                    sql += $" and crnt_balance {request.BalanceOperator} {request.BalanceValue}";
                }

                if (request.DaysChecked && !string.IsNullOrEmpty(request.DaysOperator) && !string.IsNullOrEmpty(request.DaysValue))
                {
                    sql += $" and today-fnl_date {request.DaysOperator} {request.DaysValue}";
                }

                using (var conn = new OleDbConnection(connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var record = new FinalizedAccountsRecord
                                {
                                    AccountNumber = GetStringValue(reader, "acct_number"),
                                    CurrentBalance = GetDecimalValue(reader, "crnt_balance"),
                                    CustomerName = (GetStringValue(reader, "cust_fname") + " " + GetStringValue(reader, "cust_lname")).Trim(),
                                    Address = (GetStringValue(reader, "address_1") + "," + GetStringValue(reader, "address_2") + "," + GetStringValue(reader, "address_3")).Trim().Trim(','),
                                    LastReadDate = GetDateStringValue(reader, "last_rd_dt"),
                                    FinalizedDate = GetDateStringValue(reader, "fnl_date"),
                                    MeterNo1 = GetStringValue(reader, "met_no_1"),
                                    LastRead1 = GetStringValue(reader, "last_rd_1"),
                                    MeterNo2 = GetStringValue(reader, "met_no_2"),
                                    LastRead2 = GetStringValue(reader, "last_rd_2"),
                                    MeterNo3 = GetStringValue(reader, "met_no_3"),
                                    LastRead3 = GetStringValue(reader, "last_rd_3"),
                                    SecurityDeposit = GetDecimalValue(reader, "sec_deposit")
                                };
                                response.Records.Add(record);
                            }
                        }
                    }
                }

                response.RecordCount = response.Records.Count;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching finalized accounts report");
                response.ErrorMessage = ex.Message;
                if (ex is OleDbException oleEx)
                {
                    foreach (OleDbError err in oleEx.Errors)
                    {
                        response.ErrorMessage += $" | {err.Message} (Native: {err.NativeError})";
                    }
                }
            }

            return response;
        }

        private string GetStringValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var val = reader[columnName];
                return val == DBNull.Value ? string.Empty : val.ToString().Trim();
            }
            catch { return string.Empty; }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var val = reader[columnName];
                if (val == DBNull.Value) return 0;
                return Convert.ToDecimal(val);
            }
            catch { return 0; }
        }

        private string GetDateStringValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var val = reader[columnName];
                if (val == DBNull.Value) return string.Empty;
                if (val is DateTime dt) return dt.ToString("dd/MM/yyyy");
                if (DateTime.TryParse(val.ToString(), out DateTime parsed)) return parsed.ToString("dd/MM/yyyy");
                return val.ToString();
            }
            catch { return string.Empty; }
        }
    }
}
