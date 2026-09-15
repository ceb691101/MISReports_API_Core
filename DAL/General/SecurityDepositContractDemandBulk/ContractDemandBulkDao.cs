using MISReports_Api.DBAccess;
using MISReports_Api.Models.General;
using MISReports_Api.Models.SolarInformation;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Text;

namespace MISReports_Api.DAL.General.SecurityDepositContractDemandBulk
{
    public class ContractDemandBulkDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // Use bulk connection
        }

        public List<SecDepositConDemandBulkModel> GetContractDemandBulkReport(SecDepositConDemandRequest request)
        {
            var results = new List<SecDepositConDemandBulkModel>();

            try
            {
                logger.Info("=== START GetContractDemandBulkReport ===");
                logger.Info($"Request: BillCycle={request.BillCycle}, ReportType={request.ReportType}, " +
                           $"AreaCode={request.AreaCode}, ProvCode={request.ProvCode}");

                using (var conn = _dbConnection.GetConnection(true)) // Use bulk connection
                {
                    conn.Open();

                    if (request.ReportType == SolarReportType.Area)
                    {
                        results = GetAreaReportData(conn, request);
                    }
                    else if (request.ReportType == SolarReportType.Province)
                    {
                        results = GetProvinceReportData(conn, request);
                    }
                    else
                    {
                        logger.Warn($"Unsupported report type for this DAO: {request.ReportType}");
                    }

                    logger.Info($"=== END GetContractDemandBulkReport (Success) - {results.Count} records ===");
                    return results;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching contract demand bulk report");
                throw;
            }
        }

        /// <summary>
        /// Gets data for Area report type
        /// </summary>
        private List<SecDepositConDemandBulkModel> GetAreaReportData(OleDbConnection conn, SecDepositConDemandRequest request)
        {
            var results = new List<SecDepositConDemandBulkModel>();

            try
            {
                string sql = @"SELECT c.acc_nbr, c.name, c.address_l1, c.address_l2, c.city, c.tariff, 
                                      c.cntr_dmnd, c.tot_sec_dep, m.tot_untskwo, m.tot_untskwd, 
                                      m.tot_untskwp, m.tot_kva, m.tot_amt 
                               FROM customer c
                               INNER JOIN mon_tot m ON c.acc_nbr = m.acc_nbr
                               WHERE m.bill_cycle = ? 
                                 AND c.cst_st = '0' 
                                 AND c.area_cd = ? 
                               ORDER BY c.acc_nbr";

                logger.Info($"Executing Area SQL with BillCycle={request.BillCycle}, AreaCode={request.AreaCode}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                    cmd.Parameters.AddWithValue("@area_cd", request.AreaCode);

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

                logger.Info($"Retrieved {results.Count} records for Area report");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Area report data");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Gets data for Province report type with area and province information
        /// </summary>
        private List<SecDepositConDemandBulkModel> GetProvinceReportData(OleDbConnection conn, SecDepositConDemandRequest request)
        {
            var results = new List<SecDepositConDemandBulkModel>();

            try
            {
                string sql = @"SELECT c.acc_nbr, c.name, c.address_l1, c.address_l2, c.city, c.tariff, 
                                      c.cntr_dmnd, c.tot_sec_dep, m.tot_untskwo, m.tot_untskwd, 
                                      m.tot_untskwp, m.tot_kva, m.tot_amt, 
                                      a.area_code, a.area_name,
                                      p.prov_code, p.prov_name
                               FROM customer c
                               INNER JOIN mon_tot m ON c.acc_nbr = m.acc_nbr
                               INNER JOIN areas a ON c.area_cd = a.area_code
                               INNER JOIN provinces p ON a.prov_code = p.prov_code
                               WHERE m.bill_cycle = ? 
                                 AND c.cst_st = '0' 
                                 AND p.prov_code = ? 
                               ORDER BY a.area_code, c.acc_nbr";

                logger.Info($"Executing Province SQL with BillCycle={request.BillCycle}, ProvCode={request.ProvCode}");

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@bill_cycle", request.BillCycle);
                    cmd.Parameters.AddWithValue("@prov_code", request.ProvCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var model = MapReaderToModel(reader);

                            model.AreaCode = GetColumnValue(reader, "area_code") ?? "";
                            model.Area = GetColumnValue(reader, "area_name") ?? "";
                            model.ProvinceCode = GetColumnValue(reader, "prov_code") ?? "";
                            model.Province = GetColumnValue(reader, "prov_name") ?? "";
                            model.BillCycle = request.BillCycle;

                            results.Add(model);
                        }
                    }
                }

                logger.Info($"Retrieved {results.Count} records for Province report");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching Province report data");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Maps database reader to model using ordinal positions.
        /// Columns 0-5: string fields. Columns 6-12: numeric fields.
        /// </summary>
        private SecDepositConDemandBulkModel MapReaderToModel(OleDbDataReader reader)
        {
            var model = new SecDepositConDemandBulkModel();

            try
            {
                model.AccountNumber = reader.IsDBNull(0) ? "" : reader.GetString(0).Trim();
                model.Name = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim();

                string addr1 = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim();
                string addr2 = reader.IsDBNull(3) ? "" : reader.GetString(3).Trim();
                model.Address = string.IsNullOrEmpty(addr2) ? addr1 : $"{addr1} {addr2}";

                model.City = reader.IsDBNull(4) ? "" : reader.GetString(4).Trim();
                model.Tariff = reader.IsDBNull(5) ? "" : reader.GetString(5).Trim();

                // Raw numeric values
                model.RawContractDemand = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6));
                model.RawSecurityDeposit = reader.IsDBNull(7) ? 0 : Convert.ToDecimal(reader.GetValue(7));
                model.RawTotalKWOUnits = reader.IsDBNull(8) ? 0 : Convert.ToDecimal(reader.GetValue(8));
                model.RawTotalKWDUnits = reader.IsDBNull(9) ? 0 : Convert.ToDecimal(reader.GetValue(9));
                model.RawTotalKWPUnits = reader.IsDBNull(10) ? 0 : Convert.ToDecimal(reader.GetValue(10));
                model.RawKVA = reader.IsDBNull(11) ? 0 : Convert.ToDecimal(reader.GetValue(11));
                model.RawMonthlyCharge = reader.IsDBNull(12) ? 0 : Convert.ToDecimal(reader.GetValue(12));

                // Formatted display values
                model.ContractDemand = FormatInteger(model.RawContractDemand);
                model.SecurityDeposit = FormatDecimal(model.RawSecurityDeposit);
                model.TotalKWOUnits = FormatInteger(model.RawTotalKWOUnits);
                model.TotalKWDUnits = FormatInteger(model.RawTotalKWDUnits);
                model.TotalKWPUnits = FormatInteger(model.RawTotalKWPUnits);
                model.KVA = FormatInteger(model.RawKVA);
                model.MonthlyCharge = FormatDecimal(model.RawMonthlyCharge);

                model.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error mapping reader to model");
                model.ErrorMessage = ex.Message;
            }

            return model;
        }

        private string GetColumnValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return null;
                return reader.GetString(ordinal).Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return null;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return 0;
                return Convert.ToDecimal(reader.GetValue(ordinal));
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return 0;
            }
            catch (FormatException ex)
            {
                logger.Warn(ex, $"Invalid decimal format in column '{columnName}'");
                return 0;
            }
        }

        private string FormatInteger(decimal value)
        {
            try
            {
                return ((int)value).ToString("###,###");
            }
            catch
            {
                return "0";
            }
        }

        private string FormatDecimal(decimal value)
        {
            try
            {
                return value.ToString("###,###.##");
            }
            catch
            {
                return "0.00";
            }
        }
    }
}