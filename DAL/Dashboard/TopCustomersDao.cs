using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.Dashboard
{
    public class TopCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true);
        }

        public string GetLatestBillCycle()
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();
                    return GetMaxBillCycle(conn);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching latest bill cycle from mon_tot");
                return string.Empty;
            }
        }

        public TopCustomersResponse GetTopCustomers(string billCycle = null, string region = null, string province = null, string area = null, int take = 0)
        {
            var response = new TopCustomersResponse
            {
                BillCycle = string.Empty,
                Records = new List<TopCustomerRecord>(),
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string targetBillCycle = ResolveTargetBillCycle(conn, billCycle);
                    int safeTake = take <= 0 ? 100 : take;
                    response.BillCycle = targetBillCycle;

                    if (string.IsNullOrWhiteSpace(targetBillCycle))
                    {
                        response.ErrorMessage = "No bill cycle found in mon_tot.";
                        return response;
                    }

                    var records = GetTopCustomersFromMonTot(conn, targetBillCycle, region, province, area);
                    records = records.Take(safeTake).ToList();

                    response.Records = records;
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching top customers from mon_tot");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private string GetMaxBillCycle(OleDbConnection conn)
        {
            const string sql = "SELECT MAX(BILL_CYCLE) FROM MON_TOT";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : result.ToString().Trim();
            }
        }

        private string ResolveTargetBillCycle(OleDbConnection conn, string billCycle)
        {
            if (!string.IsNullOrWhiteSpace(billCycle))
            {
                return billCycle.Trim();
            }

            return GetMaxBillCycle(conn);
        }

        private List<TopCustomerRecord> GetTopCustomersFromMonTot(OleDbConnection conn, string billCycle, string region, string province, string area)
        {
            var records = new List<TopCustomerRecord>();
            bool hasAreaFilter = !string.IsNullOrWhiteSpace(area);
            bool hasProvinceFilter = !string.IsNullOrWhiteSpace(province);
            bool hasRegionFilter = !string.IsNullOrWhiteSpace(region);
            bool hasFilter = hasAreaFilter || hasProvinceFilter || hasRegionFilter;
            string filterColumn = hasAreaFilter ? "area_code" : (hasProvinceFilter ? "prov_code" : "region");
            string resolvedProv = hasProvinceFilter ? province.Trim().ToUpperInvariant() : null;
            if (hasProvinceFilter && resolvedProv.Length == 1 && char.IsDigit(resolvedProv[0]))
            {
                resolvedProv = "0" + resolvedProv;
            }
            string filterValue = hasAreaFilter ? area.Trim().ToUpperInvariant() :
                                 (hasProvinceFilter ? resolvedProv : 
                                 (hasRegionFilter ? region.Trim().ToUpperInvariant() : null));

            string sql;
            if (hasFilter)
            {
                sql = $@"
                    SELECT m.acc_nbr,
                           c.name,
                           c.address_l1,
                           c.address_l2,
                           c.city,
                           SUM(COALESCE(m.tot_untskwo, 0) + COALESCE(m.tot_untskwp, 0) + COALESCE(m.tot_untskwd, 0)) AS kwh,
                           m.tot_amt
                    FROM mon_tot m, customer c, areas a
                    WHERE m.acc_nbr = c.acc_nbr
                    AND c.area_cd = a.area_code
                    AND m.bill_cycle = ?
                    AND a.{filterColumn} = ?
                    GROUP BY m.acc_nbr, c.name, c.address_l1, c.address_l2, c.city, m.tot_amt
                    ORDER BY kwh DESC, m.acc_nbr";
            }
            else
            {
                sql = @"
                    SELECT m.acc_nbr,
                           c.name,
                           c.address_l1,
                           c.address_l2,
                           c.city,
                           SUM(COALESCE(m.tot_untskwo, 0) + COALESCE(m.tot_untskwp, 0) + COALESCE(m.tot_untskwd, 0)) AS kwh,
                           m.tot_amt
                    FROM mon_tot m, customer c
                    WHERE m.acc_nbr = c.acc_nbr
                    AND m.bill_cycle = ?
                    GROUP BY m.acc_nbr, c.name, c.address_l1, c.address_l2, c.city, m.tot_amt
                    ORDER BY kwh DESC, m.acc_nbr";
            }

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("?", billCycle);
                if (hasFilter)
                {
                    cmd.Parameters.AddWithValue("?", filterValue);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        records.Add(new TopCustomerRecord
                        {
                            AccountNumber = GetColumnValue(reader, "acc_nbr"),
                            Name = GetColumnValue(reader, "name"),
                            AddressLine1 = GetColumnValue(reader, "address_l1"),
                            AddressLine2 = GetColumnValue(reader, "address_l2"),
                            City = GetColumnValue(reader, "city"),
                            Kwh = GetDecimalValue(reader, "kwh"),
                            TotalAmount = GetDecimalValue(reader, "tot_amt")
                        });
                    }
                }
            }

            return records;
        }

        private static string GetColumnValue(OleDbDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == null || value == DBNull.Value ? string.Empty : value.ToString().Trim();
        }

        private static decimal GetDecimalValue(OleDbDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == null || value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }
    }
}