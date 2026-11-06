using MISReports_Api.Compat;
using MISReports_Api.DBAccess;
using MISReports_Api.Models.PUCSLReports;
using System;
using System.Data.Common;

namespace MISReports_Api.DAL.PUCSLReports
{
    public class SolarJournalsRetailDao
    {
        public static void ValidateRequest(SolarJournalsRetailRequest request)
        {
            if (request == null) throw new ArgumentException("Request body is required.");
            int cycle;
            if (!int.TryParse(request.BillCycle, out cycle) || cycle <= 0)
                throw new ArgumentException("Bill cycle must be a positive integer.");
            if (request.AreaType != "Area" && request.AreaType != "Province" &&
                request.AreaType != "Division" && request.AreaType != "Entire CEB")
                throw new ArgumentException("Area type must be Area, Province, Division, or Entire CEB.");
            if (request.AreaType != "Entire CEB" && string.IsNullOrWhiteSpace(request.LocationCode))
                throw new ArgumentException("Location code is required for the selected area type.");
        }

        public static DbCommand CreateCommand(SolarJournalsRetailRequest request)
        {
            ValidateRequest(request);
            string locationFilter;
            switch (request.AreaType)
            {
                case "Area": locationFilter = " AND a.area_code = ?"; break;
                case "Province": locationFilter = " AND a.prov_code = ?"; break;
                case "Division": locationFilter = " AND a.region = ?"; break;
                default: locationFilter = ""; break;
            }

            var command = Informix.CreateCommand(@"
                SELECT a.area_name, p.acct_number, t.transac_desc, p.transac_amt,
                       p.transac_code, p.crdt_code
                FROM prn_dat_2 p, areas a, transac_types t
                WHERE p.bill_cycle = ?
                  AND p.transac_code IN ('JM','JT','JR')
                  AND p.crdt_code IN ('C','D')
                  AND a.area_code = p.area_code
                  AND t.transac_code = p.transac_code" + locationFilter + " ORDER BY 1, 2");
            command.CommandTimeout = 300;
            // OleDb binds parameters by position, with bill cycle first.
            command.Parameters.Add("billCycle", OleDbType.Integer).Value = int.Parse(request.BillCycle);
            if (request.AreaType != "Entire CEB")
                command.Parameters.Add("locationCode", OleDbType.VarChar).Value = request.LocationCode.Trim();
            return command;
        }

        public SolarJournalsRetailResponse GetReport(SolarJournalsRetailRequest request)
        {
            var result = new SolarJournalsRetailResponse();
            using (var command = CreateCommand(request))
            using (var connection = new DBConnection().GetConnection(false)) // billsmry
            {
                command.Connection = connection;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new SolarJournalsRetailRow
                        {
                            AreaName = reader.IsDBNull(0) ? "" : reader.GetValue(0).ToString().Trim(),
                            AccountNumber = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString().Trim(),
                            TransactionDescription = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString().Trim(),
                            TransactionAmount = reader.IsDBNull(3) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(3)),
                            TransactionCode = reader.IsDBNull(4) ? "" : reader.GetValue(4).ToString().Trim(),
                            CreditDebitCode = reader.IsDBNull(5) ? "" : reader.GetValue(5).ToString().Trim()
                        };
                        result.Rows.Add(row);
                        result.TotalAmount += row.TransactionAmount ?? 0m;
                        result.TotalCredit += row.CreditAmount ?? 0m;
                        result.TotalDebit += row.DebitAmount ?? 0m;
                    }
                }
            }
            return result;
        }
    }
}
