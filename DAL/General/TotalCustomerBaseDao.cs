using MISReports_Api.Compat;
using MISReports_Api.DBAccess;
using MISReports_Api.Helpers;
using MISReports_Api.Models.General;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace MISReports_Api.DAL.General
{
    public class TotalCustomerBaseDao
    {
        public static void ValidateRequest(TotalCustomerBaseRequest request)
        {
            if (request == null) throw new ArgumentException("Report parameters are required.");
            int cycle;
            if (!int.TryParse(request.BillCycle, out cycle) || cycle <= 0)
                throw new ArgumentException("Bill cycle must be a positive integer.");
            if (request.AreaType != "Province" && request.AreaType != "Division" && request.AreaType != "Entire CEB")
                throw new ArgumentException("Area type must be Province, Division, or Entire CEB.");
            if (request.AreaType != "Entire CEB" && string.IsNullOrWhiteSpace(request.LocationCode))
                throw new ArgumentException("Location code is required for the selected area type.");
        }

        public static DbCommand CreateCommand(TotalCustomerBaseRequest request)
        {
            ValidateRequest(request);
            // Keep the supplied c00-c61 expression and A/G versus F grouping for every scope.
            string counts = string.Join(" + ", Enumerable.Range(0, 62).Select(i => "a.c" + i.ToString("00")));
            string locationFilter = request.AreaType == "Province" ? " AND b.prov_code = ?"
                : request.AreaType == "Division" ? " AND b.region = ?" : "";
            var command = Informix.CreateCommand(@"
                SELECT c.prov_name, b.area_code, b.area_name,
                    SUM(CASE WHEN a.cust_type IN ('A','G') THEN " + counts + @" ELSE 0 END) AS AG_TOTAL,
                    SUM(CASE WHEN a.cust_type = 'F' THEN " + counts + @" ELSE 0 END) AS F_TOTAL
                FROM agesmry a
                JOIN areas b ON a.area_code = b.area_code
                JOIN prov_servers c ON b.prov_code = c.prov_code
                WHERE a.bill_cycle = ?" + locationFilter + @"
                GROUP BY c.prov_name, b.area_name, b.area_code
                ORDER BY c.prov_name, b.area_name, b.area_code");
            command.CommandTimeout = 120;
            // OleDb binds '?' parameters by position: bill cycle, then province/division.
            command.Parameters.Add("billCycle", OleDbType.Integer).Value = int.Parse(request.BillCycle);
            if (request.AreaType != "Entire CEB")
                command.Parameters.Add("locationCode", OleDbType.VarChar).Value = request.LocationCode.Trim();
            return command;
        }

        public TotalCustomerBaseResponse GetReport(TotalCustomerBaseRequest request)
        {
            request = request ?? new TotalCustomerBaseRequest();
            // With no cycle supplied, use the minimum cycle recorded across all areas.
            // Explicit cycles and scoped requests still use the same parameterized queries.
            if (string.IsNullOrWhiteSpace(request.BillCycle))
            {
                var cycles = GetBillCycles();
                if (cycles.Count == 0)
                    throw new InvalidOperationException("No bill cycle is available in areas.");
                request = new TotalCustomerBaseRequest
                {
                    AreaType = request.AreaType,
                    LocationCode = request.LocationCode,
                    BillCycle = cycles[0].Code
                };
            }
            var rows = new List<TotalCustomerBaseRow>();
            using (var command = CreateCommand(request))
            using (var connection = new DBConnection().GetConnection(false))
            {
                command.Connection = connection;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new TotalCustomerBaseRow
                        {
                            ProvinceName = reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString().Trim(),
                            AreaCode = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString().Trim(),
                            AreaName = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString().Trim(),
                            AgTotal = reader.IsDBNull(3) ? 0 : Convert.ToInt64(reader.GetValue(3)),
                            FTotal = reader.IsDBNull(4) ? 0 : Convert.ToInt64(reader.GetValue(4))
                        });
                    }
                }
            }
            int cycle = int.Parse(request.BillCycle);
            return new TotalCustomerBaseResponse
            {
                CurrentCycle = cycle,
                CurrentCycleLabel = cycle + " - " + BillCycleHelper.ConvertToMonthYear(cycle),
                PreviousCycle = cycle - 1,
                PreviousCycleLabel = cycle > 1
                    ? (cycle - 1) + " - " + BillCycleHelper.ConvertToMonthYear(cycle - 1) : "",
                Rows = rows
            };
        }

        public List<TotalCustomerBaseBillCycle> GetBillCycles()
        {
            var cycles = new List<TotalCustomerBaseBillCycle>();
            using (var connection = new DBConnection().GetConnection(false))
            using (var command = Informix.CreateCommand("SELECT MIN(bill_cycle) FROM areas", connection))
            {
                command.CommandTimeout = 120;
                connection.Open();
                var value = command.ExecuteScalar();
                if (value != null && value != DBNull.Value)
                {
                    int code = Convert.ToInt32(value);
                    if (code > 0)
                    {
                        cycles.Add(new TotalCustomerBaseBillCycle
                        {
                            Code = code.ToString(),
                            Label = code + " - " + BillCycleHelper.ConvertToMonthYear(code)
                        });
                    }
                }
            }
            return cycles;
        }
    }
}
