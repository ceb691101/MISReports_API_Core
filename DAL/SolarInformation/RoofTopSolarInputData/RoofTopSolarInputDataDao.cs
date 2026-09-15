using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace MISReports_Api.DAL.SolarInformation.RoofTopSolarInputData
{
    public class RoofTopSolarInputDataDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // -----------------------------------------------------------------------
        // Cycle-completion guard  (mirrors SolarReadingRetailDetailedDao logic)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns true when the cycle is confirmed complete for the chosen scope.
        /// cycle_1 is the first 3 chars of the selected calc_cycle string.
        /// cycle_2 comes from the areas table for the chosen scope.
        /// If cycle_1 > cycle_2 the cycle is not yet completed.
        /// </summary>
        private bool IsCycleCompleted(OleDbConnection conn, string calcCycle,
                                       string reportType, string typeCode)
        {
            try
            {
                if (string.IsNullOrEmpty(calcCycle) || calcCycle.Length < 3)
                    return true;

                string cycle1 = calcCycle.Substring(0, 3).Trim();
                string sql;

                switch (reportType?.ToLower())
                {
                    case "area":
                        sql = "SELECT bill_cycle FROM areas WHERE area_code=?";
                        break;
                    case "province":
                        sql = "SELECT MIN(bill_cycle) FROM areas WHERE prov_code=?";
                        break;
                    case "region":
                        sql = "SELECT MIN(bill_cycle) FROM areas WHERE region=?";
                        break;
                    default: // entireceb
                        sql = "SELECT MIN(bill_cycle) FROM areas";
                        break;
                }

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    if (reportType?.ToLower() != "entireceb" && !string.IsNullOrEmpty(typeCode))
                        cmd.Parameters.AddWithValue("?", typeCode);

                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        string cycle2 = result.ToString().Trim();
                        if (cycle2.Length >= 3 &&
                            string.Compare(cycle1, cycle2.Substring(0, 3).Trim(),
                                           StringComparison.Ordinal) > 0)
                        {
                            return false; // cycle not completed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "IsCycleCompleted check failed; proceeding anyway");
            }
            return true;
        }

        // -----------------------------------------------------------------------
        // Main report method
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns 8 scenario rows for the Roof Top Solar Input Data Portal report.
        /// reportType : "area" | "province" | "region" | "entireceb"
        /// typeCode   : area_code / prov_code / region value (null for entireceb)
        /// calcCycle  : numeric string, e.g. "439"
        /// </summary>
        public List<RoofTopSolarInputDataModel> GetReport(
            string calcCycle, string reportType, string typeCode)
        {
            var results = new List<RoofTopSolarInputDataModel>();

            try
            {
                logger.Info($"=== START RoofTopSolarInputDataDao.GetReport " +
                            $"calcCycle={calcCycle} reportType={reportType} typeCode={typeCode} ===");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    bool cycleOk = IsCycleCompleted(conn, calcCycle, reportType, typeCode);
                    if (!cycleOk)
                        logger.Warn("Cycle not yet completed for selected scope");

                    // --- Scenario 1 ---
                    // B/F = sum(bf_units - cf_units) where net_type='1', bf_units>cf_units,
                    //       bf_units>0, units_out<units_in
                    results.Add(new RoofTopSolarInputDataModel
                    {
                        ScenarioNumber = 1,
                        ScenarioDescription = "Net Metering, Import > Export, Energy BF > 0",
                        BF = QuerySingleLong(conn,
                            BuildSql("SELECT sum(bf_units-cf_units) FROM netmtcons",
                                     "n.net_type='1' AND bf_units>cf_units AND bf_units>0 AND units_out<units_in AND {cycle}",
                                     reportType),
                            calcCycle, typeCode)
                    });

                    // --- Scenario 2 ---
                    // Σ(Export-Import) col1 = sum(units_out - units_in)
                    //   where net_type='1', units_out>units_in
                    results.Add(new RoofTopSolarInputDataModel
                    {
                        ScenarioNumber = 2,
                        ScenarioDescription = "Net Metering, Import < Export",
                        SumExportMinusImport1 = QuerySingleLong(conn,
                            BuildSql("SELECT sum(units_out-units_in) FROM netmtcons",
                                     "n.net_type='1' AND units_out>units_in AND {cycle}",
                                     reportType),
                            calcCycle, typeCode)
                    });

                    // --- Scenario 3 ---
                    // No data columns – all empty (as per original VB logic)
                    results.Add(new RoofTopSolarInputDataModel
                    {
                        ScenarioNumber = 3,
                        ScenarioDescription = "Net Accounting, Import > Export"
                    });

                    // --- Scenario 4 ---
                    // Σ(Export-Import) col2 = sum(units_out - units_in)
                    //   where net_type='2', units_out>units_in
                    results.Add(new RoofTopSolarInputDataModel
                    {
                        ScenarioNumber = 4,
                        ScenarioDescription = "Net Accounting, Import < Export",
                        SumExportMinusImport2 = QuerySingleLong(conn,
                            BuildSql("SELECT sum(units_out-units_in) FROM netmtcons",
                                     "n.net_type='2' AND units_out>units_in AND {cycle}",
                                     reportType),
                            calcCycle, typeCode)
                    });

                    // --- Scenario 5 ---
                    long? sc5BF = QuerySingleLong(conn,
                        BuildSql("SELECT sum(bf_units-cf_units) FROM netmtcons",
                                 "n.net_type='5' AND bf_units>cf_units AND bf_units>0 AND units_out<units_in AND {cycle}",
                                 reportType),
                        calcCycle, typeCode);

                    long? sc5ImpExp = QuerySingleLong(conn,
                        BuildSql("SELECT sum(units_out-units_in) FROM netmtcons",
                                 "n.net_type='5' AND units_out>units_in AND {cycle}",
                                 reportType),
                        calcCycle, typeCode);

                    results.Add(new RoofTopSolarInputDataModel
                    {
                        ScenarioNumber = 5,
                        ScenarioDescription = "Net Accounting (Converted from Net Metering)",
                        BF = sc5BF,
                        SumExportMinusImport2 = sc5ImpExp
                    });

                    // --- Scenario 6 ---
                    // Two values from one query: sum(units_out) → SumExport
                    //                            sum(bf_units-cf_units) → BF
                    // net_type='3'
                    var sc6 = QueryTwoLongs(conn,
                        BuildSql("SELECT sum(units_out), sum(bf_units-cf_units) FROM netmtcons",
                                 "n.net_type='3' AND {cycle}",
                                 reportType),
                        calcCycle, typeCode);
                    results.Add(new RoofTopSolarInputDataModel
                    {
                        ScenarioNumber = 6,
                        ScenarioDescription = "Net +",
                        BF = sc6.Item2,         // bf_units-cf_units  → B/F column
                        SumExport = sc6.Item1   // units_out          → Σ Export column
                    });

                    // --- Scenario 7  (Import > 0) ---
                    // Two values: sum(bf_units-cf_units) + Sum(Units_in) → B/F
                    //             sum(unitsale) where units_in>0 → SumExport
                    // net_type='4', bf_units>cf_units, bf_units>0, units_out<units_in
                    var sc7bf = QueryTwoLongs(conn,
                        BuildSql("SELECT sum(bf_units-cf_units), sum(units_in) FROM netmtcons",
                                 "n.net_type='4' AND bf_units>cf_units AND bf_units>0 AND units_out<units_in AND {cycle}",
                                 reportType),
                        calcCycle, typeCode);

                    long? sc7Export = QuerySingleLong(conn,
                        BuildSql("SELECT sum(unitsale) FROM netmtcons",
                                 "n.net_type='4' AND units_in>0 AND {cycle}",
                                 reportType),
                        calcCycle, typeCode);

                    results.Add(new RoofTopSolarInputDataModel
                    {
                        ScenarioNumber = 7,
                        ScenarioDescription = "Net ++, Import > 0",
                        BF = (sc7bf.Item1 ?? 0) + (sc7bf.Item2 ?? 0),
                        SumExport = sc7Export
                    });

                    // --- Scenario 8  (Import = 0) ---
                    // Two values: sum(unitsale) → SumExport
                    //             sum(bf_units-cf_units) → BF
                    // net_type='4', units_in=0
                    var sc8 = QueryTwoLongs(conn,
                        BuildSql("SELECT sum(unitsale), sum(bf_units-cf_units) FROM netmtcons",
                                 "n.net_type='4' AND units_in=0 AND {cycle}",
                                 reportType),
                        calcCycle, typeCode);

                    results.Add(new RoofTopSolarInputDataModel
                    {
                        ScenarioNumber = 8,
                        ScenarioDescription = "Net ++, Import = 0",
                        BF = sc8.Item2,
                        SumExport = sc8.Item1
                    });

                    foreach (var row in results)
                        row.CalcCycle = calcCycle;
                }

                logger.Info($"=== END RoofTopSolarInputDataDao.GetReport ({results.Count} rows) ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error in RoofTopSolarInputDataDao.GetReport");
                throw;
            }
        }

        // -----------------------------------------------------------------------
        // SQL builder
        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds a complete SQL string for the four report scopes.
        /// For Area/Province/Division we JOIN areas table to avoid sub-queries.
        /// The {cycle} placeholder is replaced with the correct calc_cycle predicate.
        /// </summary>
        private string BuildSql(string selectFrom, string whereTemplate, string reportType)
        {
            // Replace {cycle} with the appropriate column reference
            string cycleClause;
            string joinClause = "";
            string finalWhere;

            bool needsJoin = reportType?.ToLower() == "province" ||
                             reportType?.ToLower() == "region";

            // When joining, prefix netmtcons columns with "n."
            // The selectFrom already uses bare table for area/entireceb paths – we adjust below.
            if (needsJoin)
            {
                // Replace plain table name with alias
                selectFrom = selectFrom.Replace("FROM netmtcons", "FROM netmtcons n, areas a");
                joinClause = "n.area_code=a.area_code AND ";
            }

            switch (reportType?.ToLower())
            {
                case "area":
                    cycleClause = "calc_cycle=? AND area_code=?";
                    break;
                case "province":
                    cycleClause = "n.calc_cycle=? AND a.prov_code=?";
                    break;
                case "region":
                    cycleClause = "n.calc_cycle=? AND a.region=?";
                    break;
                default: // entireceb
                    cycleClause = "calc_cycle=?";
                    break;
            }

            // Build WHERE clause: replace {cycle} token and prepend join clause if needed
            finalWhere = whereTemplate.Replace("{cycle}", cycleClause);

            if (!needsJoin)
                finalWhere = finalWhere.Replace("n.net_type", "net_type");

            // For joined queries, also fix up bare column refs in the where template
            // (the caller already writes "n." prefix in the template for clarity)
            return $"{selectFrom} WHERE {joinClause}{finalWhere}";
        }

        // -----------------------------------------------------------------------
        // Query helpers
        // -----------------------------------------------------------------------

        private long? QuerySingleLong(OleDbConnection conn, string sql,
                                      string calcCycle, string typeCode)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", calcCycle);
                    if (!string.IsNullOrEmpty(typeCode))
                        cmd.Parameters.AddWithValue("?", typeCode);

                    object result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)
                        return null;

                    return Convert.ToInt64(result);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"QuerySingleLong failed. SQL={sql}");
                return null;
            }
        }

        private Tuple<long?, long?> QueryTwoLongs(OleDbConnection conn, string sql,
                                                   string calcCycle, string typeCode)
        {
            try
            {
                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("?", calcCycle);
                    if (!string.IsNullOrEmpty(typeCode))
                        cmd.Parameters.AddWithValue("?", typeCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            long? col0 = reader.IsDBNull(0) ? (long?)null : Convert.ToInt64(reader[0]);
                            long? col1 = reader.IsDBNull(1) ? (long?)null : Convert.ToInt64(reader[1]);
                            return Tuple.Create(col0, col1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"QueryTwoLongs failed. SQL={sql}");
            }
            return Tuple.Create<long?, long?>(null, null);
        }
    }
}