using System;
using System.Collections.Generic;
using System.Linq;
using MISReports_Api.DBAccess;
using MISReports_Api.Models.Dashboard;
using NLog;
using System.Data.OleDb;
using System.Text;

namespace MISReports_Api.DAL.Dashboard
{
    public class BulkCustomersDao
    {
        private readonly DBConnection _dbConnection = new DBConnection();
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public bool TestConnection(out string errorMessage)
        {
            return _dbConnection.TestConnection(out errorMessage, true); // Use bulk connection
        }

        /// <summary>
        /// Get active customer count (cst_st='0')
        /// </summary>
        public int GetActiveCustomerCount(string region = null, string province = null, string area = null)
        {
            try
            {
                logger.Info("=== START GetActiveCustomerCount ===");

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    string sql;
                    bool hasAreaFilter = !string.IsNullOrWhiteSpace(area);
                    bool hasProvinceFilter = !string.IsNullOrWhiteSpace(province);
                    bool hasRegionFilter = !string.IsNullOrWhiteSpace(region);

                    if (hasAreaFilter)
                    {
                        sql = "SELECT COUNT(*) FROM customer WHERE cst_st='0' AND area_cd = ?";
                    }
                    else if (hasProvinceFilter)
                    {
                        sql = "SELECT COUNT(*) FROM customer c, areas a WHERE c.cst_st='0' AND c.area_cd = a.area_code AND a.prov_code = ?";
                    }
                    else if (hasRegionFilter)
                    {
                        sql = "SELECT COUNT(*) FROM customer c, areas a WHERE c.cst_st='0' AND c.area_cd = a.area_code AND a.region = ?";
                    }
                    else
                    {
                        sql = "SELECT COUNT(*) FROM customer WHERE cst_st='0'";
                    }

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        if (hasAreaFilter)
                        {
                            cmd.Parameters.AddWithValue("?", area.Trim().ToUpperInvariant());
                        }
                        else if (hasProvinceFilter)
                        {
                            string resolvedProv = province.Trim().ToUpperInvariant();
                            if (resolvedProv.Length == 1 && char.IsDigit(resolvedProv[0]))
                            {
                                resolvedProv = "0" + resolvedProv;
                            }
                            cmd.Parameters.AddWithValue("?", resolvedProv);
                        }
                        else if (hasRegionFilter)
                        {
                            cmd.Parameters.AddWithValue("?", region.Trim().ToUpperInvariant());
                        }

                        var result = cmd.ExecuteScalar();
                        int count = result != DBNull.Value ? Convert.ToInt32(result) : 0;

                        logger.Info($"Active customer count: {count}");
                        logger.Info("=== END GetActiveCustomerCount ===");

                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching active customer count");
                throw;
            }
        }

        /// <summary>
        /// Get customers with optional filters
        /// </summary>
        public List<BulkCustomerModel> GetCustomers(BulkCustomerRequest request)
        {
            var results = new List<BulkCustomerModel>();

            try
            {
                logger.Info("=== START GetCustomers ===");
                logger.Info($"Request: CustomerStatus={request.CustomerStatus}, ReportType={request.ReportType}");

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    // Step 1: Get customer data based on report type
                    var customers = GetCustomerData(conn, request);
                    logger.Info($"Retrieved {customers.Count} customer records");

                    if (customers.Count == 0)
                    {
                        logger.Info("No customer data found");
                        return results;
                    }

                    // Step 2: Get area information (province, region) in batch
                    var areaInfo = GetAreaInformationBatch(conn,
                        customers.Select(c => c.AreaCode).Distinct().Where(c => !string.IsNullOrEmpty(c)).ToList());
                    logger.Info($"Retrieved area information for {areaInfo.Count} areas");

                    // Step 3: Combine all data
                    foreach (var customerRecord in customers)
                    {
                        var model = new BulkCustomerModel
                        {
                            AccountNumber = customerRecord.AccountNumber,
                            Name = customerRecord.Name,
                            Tariff = customerRecord.Tariff,
                            CustomerStatus = customerRecord.CustomerStatus,
                            CustomerStatusDescription = GetCustomerStatusDescription(customerRecord.CustomerStatus),
                            AreaCode = customerRecord.AreaCode,
                            Address = customerRecord.Address,
                            PhoneNumber = customerRecord.PhoneNumber,
                            MobileNumber = customerRecord.MobileNumber,
                            ConnectionDate = customerRecord.ConnectionDate,
                            LastBillDate = customerRecord.LastBillDate,
                            LastPaymentDate = customerRecord.LastPaymentDate,
                            CustomerType = customerRecord.CustomerType,
                            IsActive = customerRecord.CustomerStatus == "0" ? "Yes" : "No"
                        };

                        // Add area information
                        if (!string.IsNullOrEmpty(customerRecord.AreaCode) &&
                            areaInfo.TryGetValue(customerRecord.AreaCode, out var area))
                        {
                            model.Area = area.AreaName;
                            model.Province = area.ProvinceName;
                            model.Division = area.Region;
                        }
                        else
                        {
                            model.Area = customerRecord.AreaCode;
                        }

                        model.ErrorMessage = string.Empty;
                        results.Add(model);
                    }
                }

                logger.Info($"=== END GetCustomers (Success) - {results.Count} records ===");
                return results;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching customers");
                throw;
            }
        }


        /// <summary>
        /// Get customer summary (counts by status, area, etc.)
        /// </summary>
        public BulkCustomerSummaryModel GetCustomerSummary(BulkCustomerRequest request)
        {
            try
            {
                logger.Info("=== START GetCustomerSummary ===");

                using (var conn = _dbConnection.GetConnection(true))
                {
                    conn.Open();

                    var summary = new BulkCustomerSummaryModel();
                    string sql = BuildSummarySql(request.ReportType);

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        // Add parameters based on report type
                        if (!string.IsNullOrEmpty(request.CustomerStatus))
                        {
                            cmd.Parameters.AddWithValue("@cst_st", request.CustomerStatus);
                        }

                        switch (request.ReportType)
                        {
                            case DashboardReportType.Area:
                                cmd.Parameters.AddWithValue("@area_cd", request.AreaCode);
                                break;
                            case DashboardReportType.Province:
                                cmd.Parameters.AddWithValue("@prov_code", request.ProvCode);
                                break;
                            case DashboardReportType.Region:
                                cmd.Parameters.AddWithValue("@region", request.Region);
                                break;
                                // No parameters needed for EntireCEB
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                summary.TotalCustomers = GetIntValue(reader, "total_customers");
                                summary.ActiveCustomers = GetIntValue(reader, "active_customers");
                                summary.InactiveCustomers = GetIntValue(reader, "inactive_customers");

                                if (request.ReportType != DashboardReportType.EntireCEB)
                                {
                                    summary.AreaCode = GetColumnValue(reader, "area_cd");
                                }
                            }
                        }
                    }

                    // Get area name if area code is present
                    if (!string.IsNullOrEmpty(summary.AreaCode))
                    {
                        var areaInfo = GetAreaInformationBatch(conn, new List<string> { summary.AreaCode });
                        if (areaInfo.TryGetValue(summary.AreaCode, out var area))
                        {
                            summary.Area = area.AreaName;
                            summary.Province = area.ProvinceName;
                        }
                    }

                    logger.Info($"Summary - Total: {summary.TotalCustomers}, Active: {summary.ActiveCustomers}, Inactive: {summary.InactiveCustomers}");
                    logger.Info("=== END GetCustomerSummary ===");

                    return summary;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error occurred while fetching customer summary");
                return new BulkCustomerSummaryModel
                {
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets customer data based on report type
        /// </summary>
        private List<CustomerData> GetCustomerData(OleDbConnection conn, BulkCustomerRequest request)
        {
            var results = new List<CustomerData>();

            try
            {
                string sql = BuildCustomerQuerySql(request.ReportType, request.IsPaginationEnabled);

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    // Add status parameter if specified
                    if (!string.IsNullOrEmpty(request.CustomerStatus))
                    {
                        cmd.Parameters.AddWithValue("@cst_st", request.CustomerStatus);
                    }

                    // Add location parameters based on report type
                    switch (request.ReportType)
                    {
                        case DashboardReportType.Area:
                            cmd.Parameters.AddWithValue("@area_cd", request.AreaCode);
                            break;
                        case DashboardReportType.Province:
                            cmd.Parameters.AddWithValue("@prov_code", request.ProvCode);
                            break;
                        case DashboardReportType.Region:
                            cmd.Parameters.AddWithValue("@region", request.Region);
                            break;
                            // No parameters needed for EntireCEB
                    }

                    // Add tariff filter if specified (need two parameters for the IS NULL check)
                    if (!string.IsNullOrEmpty(request.Tariff))
                    {
                        cmd.Parameters.AddWithValue("@tariff1", request.Tariff);
                        cmd.Parameters.AddWithValue("@tariff2", request.Tariff);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@tariff1", DBNull.Value);
                        cmd.Parameters.AddWithValue("@tariff2", DBNull.Value);
                    }

                    // Add pagination parameters if enabled
                    if (request.IsPaginationEnabled)
                    {
                        cmd.Parameters.AddWithValue("@offset", (request.PageNumber - 1) * request.PageSize);
                        cmd.Parameters.AddWithValue("@pageSize", request.PageSize);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = new CustomerData
                            {
                                AccountNumber = GetColumnValue(reader, "acc_nbr"),
                                Name = GetColumnValue(reader, "name")?.Trim() ?? "",
                                Tariff = GetColumnValue(reader, "tariff"),
                                CustomerStatus = GetColumnValue(reader, "cst_st"),
                                AreaCode = GetColumnValue(reader, "area_cd"),
                                Address = GetColumnValue(reader, "address")?.Trim() ?? "",
                                PhoneNumber = GetColumnValue(reader, "phone_no")?.Trim() ?? "",
                                MobileNumber = GetColumnValue(reader, "mobile_no")?.Trim() ?? "",
                                ConnectionDate = FormatDate(GetColumnValue(reader, "conn_date")),
                                LastBillDate = FormatDate(GetColumnValue(reader, "last_bill_date")),
                                LastPaymentDate = FormatDate(GetColumnValue(reader, "last_pay_date")),
                                CustomerType = GetColumnValue(reader, "cst_type")
                            };

                            results.Add(data);
                        }
                    }
                }

                logger.Info($"Retrieved {results.Count} customer records");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching customer data");
            }

            return results;
        }


        /// <summary>
        /// Builds the main customer query SQL based on report type
        /// </summary>
        private string BuildCustomerQuerySql(DashboardReportType reportType, bool withPagination)
        {
            StringBuilder sql = new StringBuilder();

            switch (reportType)
            {
                case DashboardReportType.Area:
                    sql.Append(@"SELECT c.acc_nbr, c.name, c.tariff, c.cst_st, c.area_cd, 
                                   c.address, c.phone_no, c.mobile_no, c.conn_date,
                                   c.last_bill_date, c.last_pay_date, c.cst_type
                            FROM customer c 
                            WHERE c.area_cd = ? ");
                    break;

                case DashboardReportType.Province:
                    sql.Append(@"SELECT c.acc_nbr, c.name, c.tariff, c.cst_st, c.area_cd, 
                                   c.address, c.phone_no, c.mobile_no, c.conn_date,
                                   c.last_bill_date, c.last_pay_date, c.cst_type
                            FROM customer c, areas a 
                            WHERE c.area_cd = a.area_code 
                            AND a.prov_code = ? ");
                    break;

                case DashboardReportType.Region:
                    sql.Append(@"SELECT c.acc_nbr, c.name, c.tariff, c.cst_st, c.area_cd, 
                                   c.address, c.phone_no, c.mobile_no, c.conn_date,
                                   c.last_bill_date, c.last_pay_date, c.cst_type
                            FROM customer c, areas a 
                            WHERE c.area_cd = a.area_code 
                            AND a.region = ? ");
                    break;

                case DashboardReportType.EntireCEB:
                    sql.Append(@"SELECT c.acc_nbr, c.name, c.tariff, c.cst_st, c.area_cd, 
                                   c.address, c.phone_no, c.mobile_no, c.conn_date,
                                   c.last_bill_date, c.last_pay_date, c.cst_type
                            FROM customer c 
                            WHERE 1=1 ");
                    break;
            }

            // Add status filter
            sql.Append(" AND c.cst_st = ? ");

            // Add tariff filter (need two parameters for IS NULL check)
            sql.Append(" AND (? IS NULL OR c.tariff = ?) ");

            sql.Append(" ORDER BY c.acc_nbr");

            // Add pagination if enabled
            if (withPagination)
            {
                sql.Append(" OFFSET ? ROWS FETCH NEXT ? ROWS ONLY");
            }

            return sql.ToString();
        }

        /// <summary>
        /// Builds summary SQL query
        /// </summary>
        private string BuildSummarySql(DashboardReportType reportType)
        {
            switch (reportType)
            {
                case DashboardReportType.Area:
                    return @"SELECT area_cd,
                                   COUNT(*) as total_customers,
                                   SUM(CASE WHEN cst_st='0' THEN 1 ELSE 0 END) as active_customers,
                                   SUM(CASE WHEN cst_st!='0' THEN 1 ELSE 0 END) as inactive_customers
                            FROM customer 
                            WHERE area_cd = ?
                            GROUP BY area_cd";

                case DashboardReportType.Province:
                    return @"SELECT a.prov_code as area_cd,
                                   COUNT(*) as total_customers,
                                   SUM(CASE WHEN c.cst_st='0' THEN 1 ELSE 0 END) as active_customers,
                                   SUM(CASE WHEN c.cst_st!='0' THEN 1 ELSE 0 END) as inactive_customers
                            FROM customer c, areas a 
                            WHERE c.area_cd = a.area_code 
                            AND a.prov_code = ?
                            GROUP BY a.prov_code";

                case DashboardReportType.Region:
                    return @"SELECT a.region as area_cd,
                                   COUNT(*) as total_customers,
                                   SUM(CASE WHEN c.cst_st='0' THEN 1 ELSE 0 END) as active_customers,
                                   SUM(CASE WHEN c.cst_st!='0' THEN 1 ELSE 0 END) as inactive_customers
                            FROM customer c, areas a 
                            WHERE c.area_cd = a.area_code 
                            AND a.region = ?
                            GROUP BY a.region";

                case DashboardReportType.EntireCEB:
                    return @"SELECT 
                                   'ALL' as area_cd,
                                   COUNT(*) as total_customers,
                                   SUM(CASE WHEN cst_st='0' THEN 1 ELSE 0 END) as active_customers,
                                   SUM(CASE WHEN cst_st!='0' THEN 1 ELSE 0 END) as inactive_customers
                            FROM customer";

                default:
                    throw new ArgumentException($"Unsupported report type: {reportType}");
            }
        }

        /// <summary>
        /// Gets area information (province, region) for multiple areas in batch
        /// </summary>
        private Dictionary<string, AreaInformation> GetAreaInformationBatch(OleDbConnection conn,
            List<string> areaCodes)
        {
            var result = new Dictionary<string, AreaInformation>();

            if (areaCodes == null || areaCodes.Count == 0)
                return result;

            try
            {
                var parameters = string.Join(",", areaCodes.Select((_, idx) => "?"));

                string sql = $@"SELECT a.area_code, a.area_name, p.prov_name, a.region 
                               FROM areas a, provinces p 
                               WHERE a.prov_code = p.prov_code 
                               AND a.area_code IN ({parameters})";

                using (var cmd = new OleDbCommand(sql, conn))
                {
                    foreach (var areaCode in areaCodes)
                    {
                        cmd.Parameters.AddWithValue("?", areaCode);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var areaCode = GetColumnValue(reader, "area_code");
                            if (!string.IsNullOrEmpty(areaCode))
                            {
                                var info = new AreaInformation
                                {
                                    AreaName = GetColumnValue(reader, "area_name")?.Trim() ?? "",
                                    ProvinceName = GetColumnValue(reader, "prov_name")?.Trim() ?? "",
                                    Region = GetColumnValue(reader, "region")?.Trim() ?? ""
                                };

                                result[areaCode] = info;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error fetching area information batch");
            }

            return result;
        }

        /// <summary>
        /// Get customer status description
        /// </summary>
        private string GetCustomerStatusDescription(string status)
        {
            switch (status)
            {
                case "0":
                    return "Active";
                case "1":
                    return "Inactive";
                case "2":
                    return "Suspended";
                case "3":
                    return "Disconnected";
                case "4":
                    return "Closed";
                default:
                    return "Unknown";
            }
        }

        // Helper methods
        private string GetColumnValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? null : value.ToString()?.Trim();
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return null;
            }
        }

        private int GetIntValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
            catch (IndexOutOfRangeException)
            {
                logger.Warn($"Column '{columnName}' not found in result set");
                return 0;
            }
        }

        private decimal GetDecimalValue(OleDbDataReader reader, string columnName)
        {
            try
            {
                var value = reader[columnName];
                return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
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

        private string FormatDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return "";

            if (DateTime.TryParse(dateStr, out var date))
            {
                return date.ToString("dd-MM-yyyy");
            }

            return dateStr;
        }

        // Helper classes for batch processing
        private class CustomerData
        {
            public string AccountNumber { get; set; }
            public string Name { get; set; }
            public string Tariff { get; set; }
            public string CustomerStatus { get; set; }
            public string AreaCode { get; set; }
            public string Address { get; set; }
            public string PhoneNumber { get; set; }
            public string MobileNumber { get; set; }
            public string ConnectionDate { get; set; }
            public string LastBillDate { get; set; }
            public string LastPaymentDate { get; set; }
            public string CustomerType { get; set; }
        }

        private class AreaInformation
        {
            public string AreaName { get; set; }
            public string ProvinceName { get; set; }
            public string Region { get; set; }
        }
    }
}