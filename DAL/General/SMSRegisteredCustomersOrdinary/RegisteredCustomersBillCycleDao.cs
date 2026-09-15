using MISReports_Api.DBAccess;
using MISReports_Api.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL
{
    public class RegisteredCustomersBillCycleDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public List<MonthlyCount> GetSMSCountRange(SMSUsageRequest request)
        {
            var results = new List<MonthlyCount>();
            using (var conn = _dbConnection.GetConnection(false))
            {
                conn.Open();
                string sql = BuildRangeSql(request.ReportType);
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", request.FromBillCycle);
                    cmd.Parameters.AddWithValue("?", request.ToBillCycle);
                    if (!string.Equals(request.ReportType, "entireceb", StringComparison.OrdinalIgnoreCase))
                    {
                        cmd.Parameters.AddWithValue("?", request.TypeCode);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new MonthlyCount
                            {
                                BillCycle = reader["bill_cycle"].ToString(),
                                Count = Convert.ToInt32(reader["reg_count"])
                            });
                        }
                    }
                }
            }
            return results;
        }

        private string BuildRangeSql(string reportType)
        {
            string select = "SELECT bill_cycle, count(*) as reg_count FROM prn_dat_1 ";
            string range = " WHERE bill_cycle >= ? AND bill_cycle <= ? AND tele_nol IS NOT NULL ";
            string group = " GROUP BY bill_cycle ORDER BY bill_cycle ASC";

            switch (reportType.ToLower())
            {
                case "area": return select + range + " AND area_code=? " + group;
                case "province": return select + range + " AND prov_code=? " + group;
                case "division":
                    return "SELECT p.bill_cycle, count(*) as reg_count FROM prn_dat_1 p, areas a " +
                           "WHERE p.prov_code=a.prov_code AND p.area_code=a.area_code " +
                           "AND p.bill_cycle >= ? AND p.bill_cycle <= ? AND a.region=? " +
                           "AND p.tele_nol IS NOT NULL GROUP BY p.bill_cycle ORDER BY p.bill_cycle ASC";
                default: return select + range + group;
            }
        }
    }
}