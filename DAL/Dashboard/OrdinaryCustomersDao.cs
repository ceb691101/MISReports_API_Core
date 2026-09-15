using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System;
using System.Data.OleDb;

namespace MISReports_Api.DAL.Dashboard
{
    public class OrdinaryCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, false); // Use ordinary connection
        }

        public OrdinaryCustomers GetOrdinaryCustomersCount(string region = null, string province = null, string area = null)
        {
            var result = new OrdinaryCustomers { TotalCount = 0, BillCycle = "" };

            try
            {
                logger.Info("=== START GetOrdinaryCustomersCount ===");

                using (var conn = _dbConnection.GetConnection(false)) // false = ordinary connection
                {
                    conn.Open();

                    string maxBillCycleSql = "select max(bill_cycle) from areas";
                    int maxBillCycle;

                    using (var maxCmd = new OleDbCommand(maxBillCycleSql, conn))
                    {
                        var maxCycleValue = maxCmd.ExecuteScalar();
                        if (maxCycleValue == null || maxCycleValue == DBNull.Value)
                        {
                            return result;
                        }

                        if (!int.TryParse(maxCycleValue.ToString(), out maxBillCycle))
                        {
                            return result;
                        }
                    }

                    result.BillCycle = (maxBillCycle - 1).ToString();

                    string sql = @"select sum(
     c.c00 + c.c01 + c.c02 + c.c03 + c.c04 + c.c05 + c.c06 + c.c07 + c.c08 + c.c09 +
     c.c10 + c.c11 + c.c12 + c.c13 + c.c14 + c.c15 + c.c16 + c.c17 + c.c18 + c.c19 +
     c.c20 + c.c21 + c.c22 + c.c23 + c.c24 + c.c25 + c.c26 + c.c27 + c.c28 + c.c29 +
     c.c30 + c.c31 + c.c32 + c.c33 + c.c34 + c.c35 + c.c36 + c.c37 + c.c38 + c.c39 +
     c.c40 + c.c41 + c.c42 + c.c43 + c.c44 + c.c45 + c.c46 + c.c47 + c.c48 + c.c49 +
     c.c50 + c.c51 + c.c52 + c.c53 + c.c54 + c.c55 + c.c56 + c.c57 + c.c58 + c.c59 +
     c.c60 + c.c61
     )
     from agesmry c, areas a
     where c.area_code = a.area_code
     and c.bill_cycle = (select max(bill_cycle) from areas) - 1
     and c.cust_type in ('A','G')";

                    bool hasArea = !string.IsNullOrWhiteSpace(area);
                    bool hasProvince = !string.IsNullOrWhiteSpace(province);
                    bool hasRegion = !string.IsNullOrWhiteSpace(region);

                    if (hasArea)
                    {
                        sql += " and a.area_code = ?";
                    }
                    else if (hasProvince)
                    {
                        sql += " and a.prov_code = ?";
                    }
                    else if (hasRegion)
                    {
                        sql += " and a.region = ?";
                    }

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (hasArea)
                        {
                            cmd.Parameters.AddWithValue("?", area.Trim().ToUpperInvariant());
                        }
                        else if (hasProvince)
                        {
                            string resolvedProv = province.Trim().ToUpperInvariant();
                            if (resolvedProv.StartsWith("0") && resolvedProv.Length > 1 && char.IsDigit(resolvedProv[1]))
                            {
                                resolvedProv = resolvedProv.Substring(1);
                            }
                            cmd.Parameters.AddWithValue("?", resolvedProv);
                        }
                        else if (hasRegion)
                        {
                            cmd.Parameters.AddWithValue("?", region.Trim().ToUpperInvariant());
                        }

                        var dbValue = cmd.ExecuteScalar();
                        if (dbValue != DBNull.Value && dbValue != null)
                        {
                            result.TotalCount = Convert.ToInt32(dbValue);
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Ordinary Customers count");
                throw;
            }
        }
    }
}