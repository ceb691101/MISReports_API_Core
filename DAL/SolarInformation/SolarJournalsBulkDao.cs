using MISReports_Api.Compat;
using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace MISReports_Api.DAL.SolarInformation
{
    public class SolarJournalsBulkDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private string GetSchemeName(string netType)
        {
            switch (netType?.Trim())
            {
                case "0": return "Net Type 0";
                case "2": return "Net Accounting";
                case "3": return "Net Plus";
                case "4": return "Net Plus Plus";
                default: return string.IsNullOrEmpty(netType) ? "Unknown" : $"Net Type {netType}";
            }
        }

        private string FormatAmountStatus(string amtStat)
        {
            if (string.IsNullOrWhiteSpace(amtStat)) return "";
            string trimmed = amtStat.Trim();
            if (trimmed == "-1" || trimmed == "-1.0" || trimmed.StartsWith("-1") || trimmed.ToLower().Contains("credit"))
                return "Cr";
            if (trimmed == "1" || trimmed == "1.0" || trimmed.StartsWith("1") || trimmed.ToLower().Contains("debit"))
                return "Dr";
            return trimmed;
        }

        public SolarJournalsBulkResponse GetSolarJournals(SolarJournalsBulkRequest request)
        {
            var response = new SolarJournalsBulkResponse
            {
                BillCycle = request.BillCycle,
                ReportCategory = request.ReportCategory,
                TypeCode = request.TypeCode
            };

            string category = request.ReportCategory?.Trim().ToLower() ?? "entireceb";
            string sql = "";
            Action<DbCommand> addParams = null;

            //Select the query based on the category
            if (category == "area")
            {
                sql = @"select a.area_name, t.acc_nbr, c.net_type, j.jnl_desc, t.txn_amt, t.amt_stat 
                        from transac t, customer c, areas a, jnal_typs j
                        where txn_type in ('EAJA','EPJA','ELJA') 
                          and t.added_blcy = ? 
                          and c.acc_nbr = t.acc_nbr 
                          and t.txn_type = j.jnl_type 
                          and a.area_code = ?
                          and a.area_code = c.area_cd 
                          and c.net_type in ('2','3','4') 
                        order by 1, 2, 3";

                addParams = cmd =>
                {
                    cmd.Parameters.AddWithValue("?", request.BillCycle);
                    cmd.Parameters.AddWithValue("?", request.TypeCode);
                };
            }
            else if (category == "province")
            {
                // Match both 01 and 1
                string provTrim = request.TypeCode?.Trim() ?? "";
                string provPad = provTrim;
                string provNoPad = provTrim;
                if (int.TryParse(provTrim, out int prInt))
                {
                    provPad = prInt.ToString("D2"); // "01"
                    provNoPad = prInt.ToString();   // "1"
                }

                sql = @"select a.area_name, t.acc_nbr, c.net_type, j.jnl_desc, t.txn_amt, t.amt_stat 
                        from transac t, customer c, areas a, jnal_typs j
                        where txn_type in ('EAJA','EPJA','ELJA') 
                          and t.added_blcy = ? 
                          and c.acc_nbr = t.acc_nbr 
                          and t.txn_type = j.jnl_type 
                          and (a.prov_code = ? OR a.prov_code = ?)
                          and a.area_code = c.area_cd 
                          and c.net_type in ('2','3','4') 
                        order by 1, 2, 3";

                addParams = cmd =>
                {
                    cmd.Parameters.AddWithValue("?", request.BillCycle);
                    cmd.Parameters.AddWithValue("?", provPad);
                    cmd.Parameters.AddWithValue("?", provNoPad);
                };
            }
            else if (category == "division" || category == "region")
            {
                sql = @"select a.area_name, t.acc_nbr, c.net_type, j.jnl_desc, t.txn_amt, t.amt_stat 
                        from transac t, customer c, areas a, jnal_typs j
                        where txn_type in ('EAJA','EPJA','ELJA') 
                          and t.added_blcy = ? 
                          and c.acc_nbr = t.acc_nbr 
                          and t.txn_type = j.jnl_type 
                          and a.region = ?
                          and a.area_code = c.area_cd 
                          and c.net_type in ('2','3','4') 
                        order by 1, 2, 3";

                addParams = cmd =>
                {
                    cmd.Parameters.AddWithValue("?", request.BillCycle);
                    cmd.Parameters.AddWithValue("?", request.TypeCode);
                };
            }
            else // Entire CEB
            {
                sql = @"select a.area_name, t.acc_nbr, c.net_type, j.jnl_desc, t.txn_amt, t.amt_stat 
                        from transac t, customer c, areas a, jnal_typs j
                        where txn_type in ('EAJA','EPJA','ELJA') 
                          and t.added_blcy = ? 
                          and c.acc_nbr = t.acc_nbr 
                          and t.txn_type = j.jnl_type
                          and a.area_code = c.area_cd 
                          and c.net_type in ('2','3','4') 
                        order by 1, 2, 3";

                addParams = cmd =>
                {
                    cmd.Parameters.AddWithValue("?", request.BillCycle);
                };
            }

            // Connect to the database and execute the query (Bulk = true)
            try
            {
                using (var conn = _dbConnection.GetConnection(true)) // Bulk Connection
                {
                    conn.Open();
                    using (var cmd = Informix.CreateCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 300;
                        addParams?.Invoke(cmd);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string netType = reader["net_type"]?.ToString()?.Trim();
                                decimal txnAmt = reader["txn_amt"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["txn_amt"]);

                                var item = new SolarJournalsBulkItem
                                {
                                    AreaName = reader["area_name"]?.ToString()?.Trim(),
                                    AccountNumber = reader["acc_nbr"]?.ToString()?.Trim(),
                                    NetType = netType,
                                    SchemeName = GetSchemeName(netType),
                                    JournalDescription = reader["jnl_desc"]?.ToString()?.Trim(),
                                    TxnAmount = txnAmt,
                                    AmountStatus = FormatAmountStatus(reader["amt_stat"]?.ToString())
                                };

                                response.Rows.Add(item);
                                response.TotalAmount += txnAmt;

                                if (item.AmountStatus == "Cr")
                                {
                                    response.TotalCredit += txnAmt;
                                }
                                else if (item.AmountStatus == "Dr")
                                {
                                    response.TotalDebit += txnAmt;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in GetSolarJournals");
                throw;
            }

            return response;
        }
    }
}