using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.General.ListOfGovernmentAccounts
{
    public class GovernmentAccountsDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        // billsmry uses the bulk connection (second parameter = true)
        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true);
        }



        // ── Departments dropdown ───────────────────────────────────────────────
        // Excludes rows where characters 3-4 of dep_code equal '99'
        public List<DepartmentModel> GetDepartments()
        {
            var results = new List<DepartmentModel>();

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Informix substring: dep_code[3,4] means chars starting at position 3, length 4
                    string sql = "Select dep_code,department from department where dep_code[3,4]!='99' order by dep_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new DepartmentModel
                            {
                                DepartmentCode = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim(),
                                DepartmentName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim()
                            });
                        }
                    }
                }

                logger.Info($"GetDepartments: returned {results.Count} departments.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching departments for Government Accounts.");
                throw;
            }

            return results;
        }



        // ── Report entry point ─────────────────────────────────────────────────
        public List<GovernmentAccountsModel> GetGovernmentAccountsReport(GovernmentAccountsRequest request)
        {
            var results = new List<GovernmentAccountsModel>();

            try
            {
                logger.Info("=== START GetGovernmentAccountsReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, ReportType={request.ReportType}, " +
                            $"AreaCode={request.AreaCode}, DepartmentCode={request.DepartmentCode}");

                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    if (request.ReportType == "area")
                    {
                        results = GetAreaReportData(conn, request);
                    }
                    else if (request.ReportType == "department")
                    {
                        results = GetDepartmentReportData(conn, request);
                    }
                    else
                    {
                        logger.Warn($"Unsupported report type: {request.ReportType}");
                    }

                    logger.Info($"=== END GetGovernmentAccountsReport (Success) - {results.Count} records ===");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching Government Accounts report.");
                throw;
            }

            return results;
        }

        // ── Area report ────────────────────────────────────────────────────────
        /// <summary>
        /// Returns all government accounts for the given area and bill cycle.
        /// SQL: JOIN prn_dat_1 with govt_acct on acct_number, filtered by area_code and bill_cycle.
        /// </summary>
        private List<GovernmentAccountsModel> GetAreaReportData(OleDbConnection conn, GovernmentAccountsRequest request)
        {
            var results = new List<GovernmentAccountsModel>();

            try
            {
                string sql = @"SELECT a.acct_number,
                                      a.cust_fname,
                                      a.cust_lname,
                                      a.address_1,
                                      a.address_2,
                                      a.address_3,
                                      a.crnt_balance,
                                      a.kwh_charge,
                                      a.avg_cons
                               FROM prn_dat_1 a, govt_acct b
                               WHERE a.area_code  LIKE ?
                                 AND a.bill_cycle  = ?
                                 AND a.acct_number = b.acct_number
                               ORDER BY a.acct_number";

                logger.Info($"Executing Area SQL: AreaCode={request.AreaCode}, BillCycle={request.BillCycle}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    // Use LIKE with wildcard so a 2-digit area prefix (e.g. "54")
                    // correctly matches all sub-area codes (e.g. 5400, 5401, 5402 …)
                    cmd.Parameters.AddWithValue("@area_code", request.AreaCode + "%");
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = MapReaderToModel(reader);
                            model.AreaCode = request.AreaCode;
                            model.BillCycle = request.BillCycle;
                            results.Add(model);
                        }
                    }
                }

                logger.Info($"Area report: {results.Count} records retrieved.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Area report data for Government Accounts.");
                throw;
            }

            return results;
        }

        // ── Department report ──────────────────────────────────────────────────
        /// <summary>
        /// Returns government accounts for the given area, bill cycle, and department.
        /// SQL: same JOIN as area report with the addition of b.dept = ?.
        /// </summary>
        private List<GovernmentAccountsModel> GetDepartmentReportData(OleDbConnection conn, GovernmentAccountsRequest request)
        {
            var results = new List<GovernmentAccountsModel>();

            try
            {
                string sql = @"SELECT a.acct_number,
                                      a.cust_fname,
                                      a.cust_lname,
                                      a.address_1,
                                      a.address_2,
                                      a.address_3,
                                      a.crnt_balance,
                                      a.kwh_charge,
                                      a.avg_cons
                               FROM prn_dat_1 a, govt_acct b
                               WHERE a.area_code  LIKE ?
                                 AND a.bill_cycle  = ?
                                 AND a.acct_number = b.acct_number
                                 AND b.dept        = ?
                               ORDER BY a.acct_number";

                logger.Info($"Executing Department SQL: AreaCode={request.AreaCode}, " +
                            $"BillCycle={request.BillCycle}, DepartmentCode={request.DepartmentCode}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    // Use LIKE with wildcard so a 2-digit area prefix (e.g. "54")
                    // correctly matches all sub-area codes (e.g. 5400, 5401, 5402 …)
                    cmd.Parameters.AddWithValue("@area_code", request.AreaCode + "%");
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                    cmd.Parameters.AddWithValue("@dept", request.DepartmentCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = MapReaderToModel(reader);
                            model.AreaCode = request.AreaCode;
                            model.BillCycle = request.BillCycle;
                            model.DepartmentCode = request.DepartmentCode;
                            results.Add(model);
                        }
                    }
                }

                logger.Info($"Department report: {results.Count} records retrieved.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Department report data for Government Accounts.");
                throw;
            }

            return results;
        }

        // ── Reader → model mapping ─────────────────────────────────────────────
        /// <summary>
        /// Maps a data reader row to GovernmentAccountsModel.
        /// Column order must match both SQL SELECT lists above:
        ///   0  acct_number
        ///   1  cust_fname
        ///   2  cust_lname
        ///   3  address_1
        ///   4  address_2
        ///   5  address_3
        ///   6  crnt_balance
        ///   7  kwh_charge
        ///   8  avg_cons
        /// </summary>
        private GovernmentAccountsModel MapReaderToModel(OleDbDataReader reader)
        {
            var model = new GovernmentAccountsModel();

            try
            {
                model.AccountNumber = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim();

                string firstName = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();
                string lastName = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
                model.CustomerName = string.IsNullOrEmpty(lastName)
                    ? firstName
                    : $"{firstName} {lastName}".Trim();

                string addr1 = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();
                string addr2 = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim();
                string addr3 = reader.IsDBNull(5) ? "" : reader.GetString(5).Trim();

                // System.Linq is now present — .Where() compiles correctly
                model.Address = string.Join(" ",
                    new[] { addr1, addr2, addr3 }.Where(s => !string.IsNullOrEmpty(s)));

                // Raw numeric values
                model.RawCurrentBalance = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6));
                model.RawKwhCharge = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7));
                model.RawAverageConsumption = reader.IsDBNull(8) ? 0 : Convert.ToDecimal(reader.GetValue(8));

                // Formatted display values
                model.CurrentBalance = FormatDecimal(model.RawCurrentBalance);
                model.KwhCharge = FormatDecimal(model.RawKwhCharge);
                model.AverageConsumption = FormatInteger(model.RawAverageConsumption);

                model.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping reader to GovernmentAccountsModel.");
                model.ErrorMessage = ex.Message;
            }

            return model;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private string FormatDecimal(decimal value)
        {
            try { return value.ToString("###,###.##"); }
            catch { return "0.00"; }
        }

        private string FormatInteger(decimal value)
        {
            try { return ((int)value).ToString("###,###"); }
            catch { return "0"; }
        }
    }
}