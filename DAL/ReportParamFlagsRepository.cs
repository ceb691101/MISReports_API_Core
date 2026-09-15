using System;
using System.Collections.Generic;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models;

namespace MISReports_Api.DAL
{
    public class ReportParamFlagsRepository
    {
        private readonly string _connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public ReportParamFlagsModel GetReportParamFlags(string repId)
        {
            const string sql = @"
SELECT TRIM(repid)     AS repid,
       TRIM(repname)   AS repname,
       TRIM(paramlist) AS paramlist
FROM   rep_reports_new
WHERE  UPPER(TRIM(repid)) = :repId";

            using (var conn = new OracleConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add("repId", OracleDbType.Varchar2).Value = repId?.Trim().ToUpperInvariant();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        var fetchedRepId = reader["REPID"]?.ToString()?.Trim();
                        var repName = reader["REPNAME"]?.ToString()?.Trim();
                        var paramList = reader["PARAMLIST"]?.ToString()?.Trim();

                        var flags = ParseParamList(paramList);

                        return new ReportParamFlagsModel
                        {
                            RepId = fetchedRepId,
                            RepName = repName,
                            Params = flags
                        };
                    }
                }
            }
        }

        // Parses "REGION=0&CCT=1&YEAR=1&MONTH=1&..." into a Dictionary<string, int>
        private static Dictionary<string, int> ParseParamList(string paramList)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(paramList))
                return result;

            foreach (var token in paramList.Split('&'))
            {
                var trimmed = token.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                var eqIndex = trimmed.IndexOf('=');
                if (eqIndex <= 0)
                    continue;

                var key = trimmed.Substring(0, eqIndex).Trim().ToUpperInvariant();
                var value = trimmed.Substring(eqIndex + 1).Trim();

                if (int.TryParse(value, out var intValue))
                    result[key] = intValue;
            }

            return result;
        }
    }
}