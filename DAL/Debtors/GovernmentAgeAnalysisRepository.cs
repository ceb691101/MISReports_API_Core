using MISReports_Api.Compat;
using MISReports_Api.DBAccess;
using MISReports_Api.DAL.Shared;
using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace MISReports_Api.DAL
{
    public class GovernmentAgeAnalysisRepository
    {
        private readonly DBConnection dbConnection = new DBConnection();
        private static readonly string[] AmountFields = new[] { "bal_mon_end" }
            .Concat(Enumerable.Range(0, 13).Select(month => "age_" + month))
            .Concat(new[] { "over_12", "over_24", "over_36" }).ToArray();

        public GovernmentAgeFilters GetFilters()
        {
            var filters = new GovernmentAgeFilters
            {
                sectors = new List<GovernmentSectorModel>(),
                billCycles = new List<string>()
            };
            using (var connection = dbConnection.GetConnection(true))
            {
                connection.Open();
                using (var command = Informix.CreateCommand("SELECT cat_code, cat_desc FROM category ORDER BY cat_code", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        filters.sectors.Add(new GovernmentSectorModel
                        {
                            CatCode = Text(reader, "cat_code"),
                            CatDesc = Text(reader, "cat_desc")
                        });
                }
                // Only offer cycles that exist in the bulk age table.
                using (var command = Informix.CreateCommand(@"SELECT FIRST 24 bill_cycle FROM age_info_new
                    WHERE bill_cycle IS NOT NULL GROUP BY bill_cycle ORDER BY bill_cycle DESC", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) filters.billCycles.Add(Text(reader, "bill_cycle"));
                }
            }
            return filters;
        }

        public List<GovernmentLocationModel> GetLocations(string areaType)
        {
            if (areaType.Equals("Province", StringComparison.OrdinalIgnoreCase))
                return new ProvinceDao().GetProvince()
                    .Where(province => province.ProvinceCode != "0" && province.ProvinceCode != "Z")
                    .Select(province => new GovernmentLocationModel
                    {
                        Code = province.ProvinceCode,
                        Name = province.ProvinceName
                    }).ToList();
            if (areaType.Equals("Division", StringComparison.OrdinalIgnoreCase))
                return new RegionDao().GetRegion().OrderBy(region => region.RegionCode)
                    .Select(region => new GovernmentLocationModel
                    {
                        Code = region.RegionCode,
                        Name = "Division " + region.RegionCode.TrimStart('R')
                    }).ToList();
            return new List<GovernmentLocationModel>();
        }

        public bool SectorExists(string sectorCode)
        {
            using (var connection = dbConnection.GetConnection(true))
            using (var command = Informix.CreateCommand("SELECT COUNT(*) FROM category WHERE cat_code = ?", connection))
            {
                command.Parameters.Add("sectorCode", OleDbType.VarChar).Value = sectorCode;
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public object GetReport(string billCycle, string reportType, string sectorCode,
            string areaType = "Entire CEB", string locationCode = null)
        {
            bool summary = reportType.Equals("Summary", StringComparison.OrdinalIgnoreCase);
            bool detailed = reportType.Equals("Detailed", StringComparison.OrdinalIgnoreCase);
            bool province = areaType.Equals("Province", StringComparison.OrdinalIgnoreCase);
            bool division = areaType.Equals("Division", StringComparison.OrdinalIgnoreCase);
            string amounts = string.Join(", ", AmountFields.Select(field => summary
                ? "SUM(NVL(a." + field + ", 0)) AS " + field
                : "a." + field));
            string select = summary
                ? "c.cat_code, s.cat_desc, COUNT(*) AS customer_count, " + amounts
                : @"r.area_name, s.cat_desc, c.acc_nbr, c.name, c.address_l1, c.address_l2,
                    c.city, c.cust_cd, c.tariff, c.cntr_dmnd, c.tot_sec_dep, " + amounts;
            string sql = "SELECT " + select + @" FROM age_info_new a
                JOIN customer c ON a.acc_nbr = c.acc_nbr
                LEFT JOIN category s ON c.cat_code = s.cat_code "
                + (summary ? "" : "LEFT JOIN areas r ON a.area_cd = r.area_code ")
                + "WHERE c.cust_cd = 'G' AND a.bill_cycle = ? "
                + (detailed ? "AND c.cat_code = ? " : "")
                + (province || division
                    ? "AND EXISTS (SELECT 1 FROM areas scope_area WHERE scope_area.area_code = a.area_cd AND scope_area."
                        + (province ? "prov_code" : "region") + " = ?) "
                    : "")
                + (summary ? "GROUP BY c.cat_code, s.cat_desc ORDER BY c.cat_code"
                    : "ORDER BY r.area_name, c.cat_code, c.acc_nbr");

            using (var connection = dbConnection.GetConnection(true))
            using (var command = Informix.CreateCommand(sql, connection))
            {
                command.CommandTimeout = 300;
                // OleDb binds positional parameters in the order of the placeholders.
                command.Parameters.Add("billCycle", OleDbType.VarChar).Value = billCycle;
                if (detailed) command.Parameters.Add("sectorCode", OleDbType.VarChar).Value = sectorCode;
                if (province || division) command.Parameters.Add("locationCode", OleDbType.VarChar).Value = locationCode;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (summary)
                    {
                        var rows = new List<GovernmentAgeSummary>();
                        while (reader.Read())
                        {
                            var row = new GovernmentAgeSummary
                            {
                                CatCode = Text(reader, "cat_code"),
                                CatDesc = SectorName(reader),
                                CustomerCount = Convert.ToInt32(reader["customer_count"])
                            };
                            ReadAmounts(reader, row);
                            rows.Add(row);
                        }
                        return rows;
                    }
                    var details = new List<GovernmentAgeDetail>();
                    while (reader.Read())
                    {
                        var row = new GovernmentAgeDetail
                        {
                            AreaName = Text(reader, "area_name"),
                            SectorName = SectorName(reader),
                            AccountNumber = Text(reader, "acc_nbr"),
                            CustomerName = Text(reader, "name"),
                            AddressLine1 = Text(reader, "address_l1"),
                            AddressLine2 = Text(reader, "address_l2"),
                            City = Text(reader, "city"),
                            CustomerCode = Text(reader, "cust_cd"),
                            Tariff = Text(reader, "tariff"),
                            ContractDemand = Amount(reader, "cntr_dmnd"),
                            SecurityDeposit = Amount(reader, "tot_sec_dep")
                        };
                        ReadAmounts(reader, row);
                        details.Add(row);
                    }
                    return details;
                }
            }
        }

        private static string Text(DbDataReader reader, string field)
            => reader[field] == DBNull.Value ? "" : reader[field].ToString().Trim();

        private static string SectorName(DbDataReader reader)
        {
            string name = Text(reader, "cat_desc");
            return string.IsNullOrEmpty(name) ? "Unspecified sector" : name;
        }

        private static decimal Amount(DbDataReader reader, string field)
            => reader[field] == DBNull.Value ? 0 : Convert.ToDecimal(reader[field]);

        private static void ReadAmounts(DbDataReader reader, GovernmentAgeAmounts row)
        {
            row.BalanceMonthEnd = Amount(reader, "bal_mon_end");
            row.Age0 = Amount(reader, "age_0");
            row.Age1 = Amount(reader, "age_1");
            row.Age2 = Amount(reader, "age_2");
            row.Age3 = Amount(reader, "age_3");
            row.Age4 = Amount(reader, "age_4");
            row.Age5 = Amount(reader, "age_5");
            row.Age6 = Amount(reader, "age_6");
            row.Age7 = Amount(reader, "age_7");
            row.Age8 = Amount(reader, "age_8");
            row.Age9 = Amount(reader, "age_9");
            row.Age10 = Amount(reader, "age_10");
            row.Age11 = Amount(reader, "age_11");
            row.Age12 = Amount(reader, "age_12");
            row.Over12 = Amount(reader, "over_12");
            row.Over24 = Amount(reader, "over_24");
            row.Over36 = Amount(reader, "over_36");
        }
    }
}
