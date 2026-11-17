using MISReports_Api.Compat;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace MISReports_Api.DAL.General
{
    public class SecurityDepositInterestDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string[] Months = {
            "Jan", "Feb", "Mar", "Apr", "May", "Jun",
            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
        };

        public static (string Label, int Year) FormatBillCycle(int cycle)
        {
            if (cycle <= 0) return ("", 0);
            int baseYear = 1988;
            int baseMonth = 8;
            int totalMonths = baseMonth + (cycle - 1);
            int year = baseYear + (totalMonths / 12);
            int month = totalMonths % 12;
            string label = $"{cycle} - {Months[month]} {year}";
            return (label, year);
        }

        private DbConnection GetBulkConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings["InformixBulkConnection"];
            string connStr = setting?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new ConfigurationErrorsException("Connection string \'InformixBulkConnection\' is missing from connectionStrings.config.");
            }
            return Informix.CreateConnection(connStr);
        }

        private DbConnection GetOrdinaryConnection()
        {
            var setting = ConfigurationManager.ConnectionStrings["InformixConnection"];
            string connStr = setting?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new ConfigurationErrorsException("Connection string \'InformixConnection\' is missing from connectionStrings.config.");
            }
            return Informix.CreateConnection(connStr);
        }

        public List<SecurityDepositBillCycleOption> GetBulkBillCycles(int count = 36)
        {
            var list = new List<SecurityDepositBillCycleOption>();
            int maxCycle = 0;

            try
            {
                using (var conn = GetBulkConnection())
                {
                    conn.Open();
                    using (var cmd = Informix.CreateCommand("select max(added_blcy) from transac", conn))
                    {
                        cmd.CommandTimeout = 120;
                        var res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value && int.TryParse(res.ToString(), out int parsed))
                        {
                            maxCycle = parsed;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching max bill cycle for Bulk from transac");
                throw;
            }

            if (maxCycle > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    int c = maxCycle - i;
                    if (c <= 0) break;
                    var (label, _) = FormatBillCycle(c);
                    list.Add(new SecurityDepositBillCycleOption
                    {
                        BillCycle = c,
                        Code = c.ToString(),
                        Display = label
                    });
                }
            }

            return list;
        }

        public List<SecurityDepositBillCycleOption> GetOrdinaryBillCycles()
        {
            var list = new List<SecurityDepositBillCycleOption>();

            try
            {
                using (var conn = GetOrdinaryConnection())
                {
                    conn.Open();
                    string sql = "select distinct bill_cycle from transmry where tran_code = 'NB' order by bill_cycle desc";
                    using (var cmd = Informix.CreateCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 120;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (int.TryParse(reader["bill_cycle"]?.ToString(), out int c))
                                {
                                    var (label, _) = FormatBillCycle(c);
                                    list.Add(new SecurityDepositBillCycleOption
                                    {
                                        BillCycle = c,
                                        Code = c.ToString(),
                                        Display = label
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching distinct bill cycles for Ordinary from transmry");
                throw;
            }

            return list;
        }

        public decimal? GetInterestRate(int year)
        {
            try
            {
                using (var conn = GetBulkConnection())
                {
                    conn.Open();
                    string sql = "select rate from sec_dep_int_rate where year(from_date) = ?";
                    using (var cmd = Informix.CreateCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 60;
                        cmd.Parameters.AddWithValue("?", year);
                        var res = cmd.ExecuteScalar();
                        if (res != null && res != DBNull.Value && decimal.TryParse(res.ToString(), out decimal rate))
                        {
                            return rate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Could not retrieve interest rate for year {year} from billhsbhq. Trying billsmry...");
                try
                {
                    using (var conn = GetOrdinaryConnection())
                    {
                        conn.Open();
                        string sql = "select rate from sec_dep_int_rate where year(from_date) = ?";
                        using (var cmd = Informix.CreateCommand(sql, conn))
                        {
                            cmd.CommandTimeout = 60;
                            cmd.Parameters.AddWithValue("?", year);
                            var res = cmd.ExecuteScalar();
                            if (res != null && res != DBNull.Value && decimal.TryParse(res.ToString(), out decimal rate))
                            {
                                return rate;
                            }
                        }
                    }
                }
                catch (Exception ex2)
                {
                    logger.Warn(ex2, $"Could not retrieve interest rate for year {year} from sec_dep_int_rate in billsmry");
                }
            }

            return null;
        }

        public SecurityDepositInterestResponse GetReportData(SecurityDepositInterestRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var response = new SecurityDepositInterestResponse
            {
                ReportType = request.ReportType,
                BillCycle = request.BillCycle,
                Category = request.Category,
                CategoryValue = request.CategoryValue
            };

            var (label, year) = FormatBillCycle(request.BillCycle);
            response.BillCycleLabel = label;
            response.InterestYear = year;
            response.InterestRate = GetInterestRate(year);

            bool isBulk = string.Equals(request.ReportType, "Bulk", StringComparison.OrdinalIgnoreCase);

            if (isBulk)
            {
                FetchBulkReportData(request, response);
            }
            else
            {
                FetchOrdinaryReportData(request, response);
            }

            decimal sum = 0;
            foreach (var r in response.Rows)
            {
                sum += r.TotalAmount;
            }
            response.GrandTotal = sum;

            return response;
        }

        private void FetchBulkReportData(SecurityDepositInterestRequest request, SecurityDepositInterestResponse response)
        {
            string category = request.Category?.Trim() ?? "Entire CEB";
            string catVal = request.CategoryValue?.Trim() ?? "";

            string sql;
            DbParameter[] parameters;

            if (category.Equals("Area", StringComparison.OrdinalIgnoreCase))
            {
                sql = @"select d.tariff_desc, SUM(t.txn_amt) as total_amt 
                        from transac t, customer c, tariff_desc d, areas a 
                        where t.acc_nbr = c.acc_nbr 
                          and t.added_blcy = ? 
                          and t.txn_type = 'ISDA' 
                          and c.tariff = d.tariff 
                          and a.area_code = c.area_cd 
                          and a.area_code = ? 
                        group by 1 order by 1";
                parameters = new[]
                {
                    Informix.CreateParameter("?", request.BillCycle),
                    Informix.CreateParameter("?", catVal)
                };
            }
            else if (category.Equals("Province", StringComparison.OrdinalIgnoreCase))
            {
                sql = @"select d.tariff_desc, SUM(t.txn_amt) as total_amt 
                        from transac t, customer c, tariff_desc d, areas a 
                        where t.acc_nbr = c.acc_nbr 
                          and t.added_blcy = ? 
                          and t.txn_type = 'ISDA' 
                          and c.tariff = d.tariff 
                          and a.area_code = c.area_cd 
                          and a.prov_code = ? 
                        group by 1 order by 1";
                parameters = new[]
                {
                    Informix.CreateParameter("?", request.BillCycle),
                    Informix.CreateParameter("?", catVal)
                };
            }
            else if (category.Equals("Division", StringComparison.OrdinalIgnoreCase))
            {
                sql = @"select d.tariff_desc, SUM(t.txn_amt) as total_amt 
                        from transac t, customer c, tariff_desc d, areas a 
                        where t.acc_nbr = c.acc_nbr 
                          and t.added_blcy = ? 
                          and t.txn_type = 'ISDA' 
                          and c.tariff = d.tariff 
                          and a.area_code = c.area_cd 
                          and a.region = ? 
                        group by 1 order by 1";
                parameters = new[]
                {
                    Informix.CreateParameter("?", request.BillCycle),
                    Informix.CreateParameter("?", catVal)
                };
            }
            else // Entire CEB
            {
                sql = @"select d.tariff_desc, SUM(t.txn_amt) as total_amt 
                        from transac t, customer c, tariff_desc d 
                        where t.acc_nbr = c.acc_nbr 
                          and t.added_blcy = ? 
                          and t.txn_type = 'ISDA' 
                          and c.tariff = d.tariff 
                        group by 1 order by 1";
                parameters = new[]
                {
                    Informix.CreateParameter("?", request.BillCycle)
                };
            }

            using (var conn = GetBulkConnection())
            {
                conn.Open();
                using (var cmd = Informix.CreateCommand(sql, conn))
                {
                    cmd.CommandTimeout = 300;
                    cmd.Parameters.AddRange(parameters);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tDesc = reader[0]?.ToString()?.Trim() ?? "Unknown";
                            decimal amt = 0;
                            if (reader[1] != DBNull.Value)
                            {
                                decimal.TryParse(reader[1].ToString(), out amt);
                            }

                            response.Rows.Add(new SecurityDepositInterestItem
                            {
                                TariffName = tDesc,
                                TotalAmount = amt
                            });
                        }
                    }
                }
            }
        }

        private void FetchOrdinaryReportData(SecurityDepositInterestRequest request, SecurityDepositInterestResponse response)
        {
            string category = request.Category?.Trim() ?? "Entire CEB";
            string catVal = request.CategoryValue?.Trim() ?? "";

            string sql;
            DbParameter[] parameters;

            if (category.Equals("Area", StringComparison.OrdinalIgnoreCase))
            {
                sql = @"select b.tariff_name, sum(a.tran_amt) as total_amt 
                        from transmry a, tariff_code b 
                        where a.tran_code = 'NB' 
                          and a.bill_cycle = ? 
                          and a.area_code = ? 
                          and a.tariff_code = b.tariff_code 
                        group by b.tariff_name order by b.tariff_name";
                parameters = new[]
                {
                    Informix.CreateParameter("?", request.BillCycle),
                    Informix.CreateParameter("?", catVal)
                };
            }
            else if (category.Equals("Province", StringComparison.OrdinalIgnoreCase))
            {
                sql = @"select b.tariff_name, sum(a.tran_amt) as total_amt 
                        from transmry a, tariff_code b, areas c 
                        where a.tran_code = 'NB' 
                          and a.bill_cycle = ? 
                          and c.prov_code = ? 
                          and a.tariff_code = b.tariff_code 
                          and a.area_code = c.area_code 
                        group by b.tariff_name order by b.tariff_name";
                parameters = new[]
                {
                    Informix.CreateParameter("?", request.BillCycle),
                    Informix.CreateParameter("?", catVal)
                };
            }
            else if (category.Equals("Division", StringComparison.OrdinalIgnoreCase))
            {
                sql = @"select b.tariff_name, sum(a.tran_amt) as total_amt 
                        from transmry a, tariff_code b, areas c 
                        where a.tran_code = 'NB' 
                          and a.bill_cycle = ? 
                          and c.region = ? 
                          and a.tariff_code = b.tariff_code 
                          and a.area_code = c.area_code 
                        group by b.tariff_name order by b.tariff_name";
                parameters = new[]
                {
                    Informix.CreateParameter("?", request.BillCycle),
                    Informix.CreateParameter("?", catVal)
                };
            }
            else // Entire CEB
            {
                sql = @"select b.tariff_name, sum(a.tran_amt) as total_amt 
                        from transmry a, tariff_code b 
                        where a.tran_code = 'NB' 
                          and a.bill_cycle = ? 
                          and a.tariff_code = b.tariff_code 
                        group by b.tariff_name order by b.tariff_name";
                parameters = new[]
                {
                    Informix.CreateParameter("?", request.BillCycle)
                };
            }

            using (var conn = GetOrdinaryConnection())
            {
                conn.Open();
                using (var cmd = Informix.CreateCommand(sql, conn))
                {
                    cmd.CommandTimeout = 300;
                    cmd.Parameters.AddRange(parameters);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tName = reader[0]?.ToString()?.Trim() ?? "Unknown";
                            decimal amt = 0;
                            if (reader[1] != DBNull.Value)
                            {
                                decimal.TryParse(reader[1].ToString(), out amt);
                            }

                            response.Rows.Add(new SecurityDepositInterestItem
                            {
                                TariffName = tName,
                                TotalAmount = amt
                            });
                        }
                    }
                }
            }
        }
    }
}

