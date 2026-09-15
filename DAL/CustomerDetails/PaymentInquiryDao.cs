using MISReports_Api.DBAccess;
using MISReports_Api.Models.CustomerDetails;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Linq;

namespace MISReports_Api.DAL.CustomerDetails
{
    public class PaymentInquiryDao
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string PmntConnectionName = "InformixPmntConsld";
        private const string BillsmryConnectionName = "InformixConnection";
        private const string BulkConnectionName = "InformixBulkConnection";

        public bool TestConnection(out string errorMessage)
        {
            errorMessage = null;

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public LatestUpdateTimeResponse GetLatestUpdateTimes()
        {
            var response = new LatestUpdateTimeResponse
            {
                Records = new List<LatestUpdateTimeRecord>(),
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();
                    response.Records = GetLatestUpdateTimes(conn);
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching latest update times");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private List<LatestUpdateTimeRecord> GetLatestUpdateTimes(OleDbConnection conn)
        {
            var records = new List<LatestUpdateTimeRecord>();

            const string sql = @"
				SELECT a.agent,
					   a.center,
					   a.last_update,
					   b.agent_name,
					   b.center_name
				FROM latest_updates a, agent_details b
				WHERE a.agent = b.agent
				  AND a.center = b.center
				ORDER BY a.agent, a.center";

            using (var cmd = new OleDbCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    records.Add(new LatestUpdateTimeRecord
                    {
                        Agent = GetStringValue(reader, "agent"),
                        Center = GetStringValue(reader, "center"),
                        LastUpdate = GetDateTimeValue(reader, "last_update"),
                        AgentName = GetStringValue(reader, "agent_name"),
                        CenterName = GetStringValue(reader, "center_name")
                    });
                }
            }

            return records;
        }

        public PaymentInquiryResponse GetFullReport(PaymentInquiryRequest request)
        {
            return BuildPaymentInquiryResponse(request);
        }

        private PaymentInquiryResponse BuildPaymentInquiryResponse(PaymentInquiryRequest request)
        {
            var response = new PaymentInquiryResponse
            {
                PaymentRecords = new List<PaymentInquiryPaymentRecord>(),
                ErrorMessage = string.Empty
            };

            try
            {
                if (request == null)
                {
                    response.ErrorMessage = "Request body is required.";
                    return response;
                }

                var accountNumber = (request.AcctNo ?? string.Empty).Trim();
                var fromDate = NormalizeDateString(request.FromDate);

                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    response.ErrorMessage = "Account number is required.";
                    return response;
                }

                if (string.IsNullOrWhiteSpace(fromDate))
                {
                    response.ErrorMessage = "From date is required.";
                    return response;
                }

                response.AccountNumber = accountNumber;
                response.FromDate = fromDate;
                response.ToDate = DateTime.Today.ToString("dd-MM-yyyy");
                //response.ToDate = DateTime.Today.ToString("yyyy-MM-dd");

                LoadSummaryFields(response, accountNumber);

                //database connection
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();
                    response.PaymentRecords = GetPaymentRecords(conn, accountNumber, fromDate);
                }

                response.TotalAmount = response.PaymentRecords.Sum(record => GetDecimalValue(record.TransAmt));
                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching full payment inquiry report");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private void LoadSummaryFields(PaymentInquiryResponse response, string accountNumber)
        {
            response.AreaName = GetAreaName(accountNumber);

            if (IsBulkAccount(accountNumber))
            {
                response.CustomerType = "Bulk";
                LoadBulkCustomer(response, accountNumber);
            }
            else
            {
                response.CustomerType = "Ordinary";
                LoadOrdinaryCustomer(response, accountNumber);
            }
        }

        private void LoadOrdinaryCustomer(PaymentInquiryResponse response, string accountNumber)
        {
            try
            {
                using (var conn = GetConnection(BillsmryConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT cust_fname,
                               cust_lname,
                               address_1,
                               address_2,
                               address_3
                        FROM prn_dat_1
                        WHERE acct_number = ?
                        ORDER BY bill_cycle DESC";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@acctNo", accountNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                var firstName = GetStringValue(reader, "cust_fname");
                                var lastName = GetStringValue(reader, "cust_lname");
                                response.CustomerName = string.Join(" ", new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();
                                response.Address1 = GetStringValue(reader, "address_1");
                                response.Address2 = GetStringValue(reader, "address_2");
                                response.Address3 = GetStringValue(reader, "address_3");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load ordinary customer summary for account {0}", accountNumber);
            }
        }

        private void LoadBulkCustomer(PaymentInquiryResponse response, string accountNumber)
        {
            try
            {
                using (var conn = GetConnection(BulkConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT name,
                               address_l1,
                               address_l2,
                               city
                        FROM customer
                        WHERE acc_nbr = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@acctNo", accountNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                response.CustomerName = GetStringValue(reader, "name");
                                response.Address1 = GetStringValue(reader, "address_l1");
                                response.Address2 = GetStringValue(reader, "address_l2");
                                response.Address3 = GetStringValue(reader, "city");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load bulk customer summary for account {0}", accountNumber);
            }
        }


        private string GetAreaName(string accountNumber)
        {
            var areaName = GetAreaNameFromBillsmry(accountNumber);
            if (!string.IsNullOrWhiteSpace(areaName))
            {
                return areaName;
            }

            return GetAreaNameFromPayments(accountNumber);
        }

        private string GetAreaNameFromBillsmry(string accountNumber)
        {
            try
            {
                using (var conn = GetConnection(BillsmryConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT b.area_name
                        FROM cust_hq a, areas b
                        WHERE a.acct_number = ?
                          AND a.area_code = b.area_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@acctNo", accountNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return GetStringValue(reader, "area_name");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load area name from billsmry for account {0}", accountNumber);
            }

            return string.Empty;
        }

        private string GetAreaNameFromPayments(string accountNumber)
        {
            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT b.area_name
                        FROM online_payments a, areas b
                        WHERE a.acc_no = ?
                          AND a.area_code = b.area_code
                        UNION ALL
                        SELECT b.area_name
                        FROM offline_payments a, areas b
                        WHERE a.acc_no = ?
                          AND a.area_code = b.area_code
                        UNION ALL
                        SELECT b.area_name
                        FROM online_bank a, areas b
                        WHERE a.acc_no = ?
                          AND a.area_code = b.area_code";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@acctNo1", accountNumber);
                        cmd.Parameters.AddWithValue("@acctNo2", accountNumber);
                        cmd.Parameters.AddWithValue("@acctNo3", accountNumber);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return GetStringValue(reader, "area_name");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load fallback area name from payments for account {0}", accountNumber);
            }

            return string.Empty;
        }

       //payment inquiry 
        private List<PaymentInquiryPaymentRecord> GetPaymentRecords(OleDbConnection conn, string accountNumber, string fromDate)
        {
            var records = new List<PaymentInquiryPaymentRecord>();

            const string sql = @"
                SELECT trans_date,
                       trans_amt,
                       center,
                       count_no,
                       pay_mode,
                       trans_time,
                       trans_type,
                       stub_no,
                       agent,
                       usr_lot
                FROM online_payments
                WHERE acc_no = ?
                  AND trans_date >= ?
                  AND trans_type = '0'
                UNION ALL
                SELECT trans_date,
                       trans_amt,
                       center,
                       count_no,
                       pay_mode,
                       trans_time,
                       trans_type,
                       stub_no,
                       agent,
                       usr_lot
                FROM offline_payments
                WHERE acc_no = ?
                  AND trans_date >= ?
                UNION ALL
                SELECT trans_date,
                       trans_amt,
                       center,
                       count_no,
                       pay_mode,
                       trans_time,
                       trans_type,
                       stub_no,
                       agent,
                       usr_lot
                FROM online_bank
                WHERE acc_no = ?
                  AND trans_date >= ?
                ORDER BY 1";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                //online payments
                cmd.Parameters.AddWithValue("@acctNo1", accountNumber);
                cmd.Parameters.AddWithValue("@fromDate1", fromDate);
                //offline payments
                cmd.Parameters.AddWithValue("@acctNo2", accountNumber);
                cmd.Parameters.AddWithValue("@fromDate2", fromDate);
                //online bank
                cmd.Parameters.AddWithValue("@acctNo3", accountNumber);
                cmd.Parameters.AddWithValue("@fromDate3", fromDate);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var transDate = GetDateValue(reader, "trans_date");
                        var transAmt = GetStringValue(reader, "trans_amt");
                        var center = GetStringValue(reader, "center");
                        var countNo = GetStringValue(reader, "count_no");
                        var payMode = GetStringValue(reader, "pay_mode");
                        var transTime = GetStringValue(reader, "trans_time");
                        var transType = GetStringValue(reader, "trans_type");
                        var stubNo = GetStringValue(reader, "stub_no");
                        var agent = GetStringValue(reader, "agent");
                        var usrLot = GetStringValue(reader, "usr_lot");

                        var agentDetails = GetAgentDetails(agent, center);
                        var counterDetails = GetCounterDetails(countNo);
                        var codeDescription = GetCodeDescription(payMode);
                        var chequeMoneyOrderNo = string.Equals(payMode, "Q", StringComparison.OrdinalIgnoreCase)
                            ? GetChequeMoneyOrderNo(accountNumber, transDate, countNo, stubNo)
                            : string.Empty;

                        records.Add(new PaymentInquiryPaymentRecord
                        {
                            TransDate = transDate,
                            TransAmt = transAmt,
                            Center = center,
                            CountNo = countNo,
                            PayMode = payMode,
                            TransTime = transTime,
                            TransType = transType,
                            StubNo = stubNo,
                            Agent = agent,
                            UsrLot = usrLot,
                            AgentName = agentDetails.AgentName,
                            CenterName = agentDetails.CenterName,
                            CounterName = counterDetails,
                            CodeDescription = codeDescription,
                            ChequeMoneyOrderNo = chequeMoneyOrderNo
                        });
                    }
                }
            }

            return records;
        }

        //latest update times of servers - agent details
        private (string AgentName, string CenterName) GetAgentDetails(string agent, string center)
        {
            if (string.IsNullOrWhiteSpace(agent) || string.IsNullOrWhiteSpace(center))
            {
                return (string.Empty, string.Empty);
            }

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT agent_name,
                               center_name
                        FROM agent_details
                        WHERE agent = ?
                          AND center = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@agent", agent);
                        cmd.Parameters.AddWithValue("@center", center);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return (GetStringValue(reader, "agent_name"), GetStringValue(reader, "center_name"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load agent details for agent {0}, center {1}", agent, center);
            }

            return (string.Empty, string.Empty);
        }

        //POS Counter Collection Breakup - counter
        private string GetCounterDetails(string countNo)
        {
            if (string.IsNullOrWhiteSpace(countNo))
            {
                return string.Empty;
            }

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT counter,
                               counter_name
                        FROM counter_details
                        WHERE counter = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@counter", countNo);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                var counter = GetStringValue(reader, "counter");
                                var counterName = GetStringValue(reader, "counter_name");
                                return string.IsNullOrWhiteSpace(counterName) ? counter : $"{counter} - {counterName}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load counter details for counter {0}", countNo);
            }

            return string.Empty;
        }

        private string GetCodeDescription(string payMode)
        {
            if (string.IsNullOrWhiteSpace(payMode))
            {
                return string.Empty;
            }

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT code_discrp
                        FROM code_paymode
                        WHERE pay_mode = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@payMode", payMode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return GetStringValue(reader, "code_discrp");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load pay mode description for pay mode {0}", payMode);
            }

            return string.Empty;
        }

        private string GetChequeMoneyOrderNo(string accountNumber, string transDate, string countNo, string stubNo)
        {
            if (string.IsNullOrWhiteSpace(accountNumber) || string.IsNullOrWhiteSpace(transDate) || string.IsNullOrWhiteSpace(countNo) || string.IsNullOrWhiteSpace(stubNo))
            {
                return string.Empty;
            }

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();

                    const string sql = @"
                        SELECT chq_mny_no
                        FROM chq_mnyord
                        WHERE acno_pivno = ?
                          AND trans_date = ?
                          AND count_no = ?
                          AND stub_no = ?";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                        cmd.Parameters.AddWithValue("@transDate", transDate);
                        cmd.Parameters.AddWithValue("@countNo", countNo);
                        cmd.Parameters.AddWithValue("@stubNo", stubNo);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader != null && reader.Read())
                            {
                                return GetStringValue(reader, "chq_mny_no");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to load cheque money order number for account {0}", accountNumber);
            }

            return string.Empty;
        }

        //if 3rd no. of acc no. is 7, it is bulk
        private static bool IsBulkAccount(string accountNumber)
        {
            return !string.IsNullOrWhiteSpace(accountNumber) && accountNumber.Length >= 3 && accountNumber[2] == '7';
        }

        private static string NormalizeDateString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (DateTime.TryParse(value, out DateTime parsedDate))
            {
                //return parsedDate.ToString("yyyy-MM-dd");
                return parsedDate.ToString("dd-MM-yyyy");
            }

            return value.Trim();
        }

        private static string GetDateValue(OleDbDataReader reader, string columnName)
        {
            object value = reader[columnName];

            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                //return dateTime.ToString("yyyy-MM-dd");
                return dateTime.ToString("dd-MM-yyyy");
            }

            if (DateTime.TryParse(value.ToString(), out DateTime parsedDateTime))
            {
                //return parsedDateTime.ToString("yyyy-MM-dd");
                return parsedDateTime.ToString("dd-MM-yyyy");
            }

            return value.ToString().Trim();
        }

        private static decimal GetDecimalValue(string value)
        {
            return decimal.TryParse(value, out decimal parsed) ? parsed : 0m;
        }

        private static OleDbConnection GetConnection(string connectionName)
        {
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName];

            if (connectionStringSettings == null || string.IsNullOrWhiteSpace(connectionStringSettings.ConnectionString))
            {
                throw new ConfigurationErrorsException($"{connectionName} connection string is missing from Web.config");
            }

            return new OleDbConnection(connectionStringSettings.ConnectionString);
        }

        private static string GetStringValue(OleDbDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == null || value == DBNull.Value ? string.Empty : value.ToString().Trim();
        }

        private static string GetDateTimeValue(OleDbDataReader reader, string columnName)
        {
            object value = reader[columnName];

            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString("dd-MM-yyyy HH:mm:ss");
                //return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            if (DateTime.TryParse(value.ToString(), out DateTime parsedDateTime))
            {
                return parsedDateTime.ToString("dd-MM-yyyy HH:mm:ss");
                //return parsedDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }

            return value.ToString().Trim();
        }

        #region POS Counter Collection Breakup

        public ProvinceListResponse GetProvinces()
        {
            var response = new ProvinceListResponse
            {
                Provinces = new List<ProvinceData>(),
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = GetConnection(BillsmryConnectionName))
                {
                    conn.Open();
                    response.Provinces = GetProvincesList(conn);
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching provinces");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private List<ProvinceData> GetProvincesList(OleDbConnection conn)
        {
            var provinces = new List<ProvinceData>();

            const string sql = @"
                SELECT prov_name,
                       TRIM(prov_code) AS prov_code,
                       TRIM(prov_svr_name) AS prov_svr_name,
                       TRIM(uname) AS uname,
                       TRIM(upwd) AS upwd
                FROM prov_servers
                ORDER BY prov_name";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.CommandTimeout = 30;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        provinces.Add(new ProvinceData
                        {
                            ProvName = GetStringValue(reader, "prov_name"),
                            ProvCode = GetStringValue(reader, "prov_code"),
                            ProvSvrName = GetStringValue(reader, "prov_svr_name"),
                            Username = GetStringValue(reader, "uname"),
                            Password = GetStringValue(reader, "upwd")
                        });
                    }
                }
            }

            return provinces;
        }

        public AreaListResponse GetAreas(string provCode)
        {
            var response = new AreaListResponse
            {
                Areas = new List<AreaData>(),
                ErrorMessage = string.Empty
            };

            try
            {
                if (string.IsNullOrWhiteSpace(provCode))
                {
                    response.ErrorMessage = "Province code is required.";
                    return response;
                }

                using (var conn = GetConnection(BillsmryConnectionName))
                {
                    conn.Open();
                    response.Areas = GetAreasList(conn, provCode);
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching areas for province {0}", provCode);
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private List<AreaData> GetAreasList(OleDbConnection conn, string provCode)
        {
            var areas = new List<AreaData>();

            const string sql = @"
                SELECT area_code,
                       area_name
                FROM areas
                WHERE prov_code = ?
                ORDER BY area_name";

            using (var cmd = new OleDbCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@provCode", provCode);
                cmd.CommandTimeout = 30;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        areas.Add(new AreaData
                        {
                            AreaCode = GetStringValue(reader, "area_code"),
                            AreaName = GetStringValue(reader, "area_name")
                        });
                    }
                }
            }

            return areas;
        }

        public CounterListResponse GetCounters(string provCode)
        {
            var response = new CounterListResponse
            {
                Counters = new List<CounterData>(),
                ErrorMessage = string.Empty
            };

            try
            {
                if (string.IsNullOrWhiteSpace(provCode))
                {
                    response.ErrorMessage = "Province code is required.";
                    return response;
                }

                var diagnosticLogs = new List<string>();
                bool anySuccess = false;

                // 1. Try InformixConnection (BillsmryConnectionName)
                string log1;
                var list1 = GetCountersFromConnection(BillsmryConnectionName, provCode, out log1, out bool success1);
                diagnosticLogs.Add(log1);
                if (success1) anySuccess = true;
                if (list1 != null && list1.Count > 0)
                {
                    response.Counters = list1;
                    return response;
                }

                // 2. Try InformixPmntConsld (PmntConnectionName)
                string log2;
                var list2 = GetCountersFromConnection(PmntConnectionName, provCode, out log2, out bool success2);
                diagnosticLogs.Add(log2);
                if (success2) anySuccess = true;
                if (list2 != null && list2.Count > 0)
                {
                    response.Counters = list2;
                    return response;
                }

                // 3. Try InformixPosPayment
                string log3;
                var list3 = GetCountersFromConnection("InformixPosPayment", provCode, out log3, out bool success3);
                diagnosticLogs.Add(log3);
                if (success3) anySuccess = true;
                if (list3 != null && list3.Count > 0)
                {
                    response.Counters = list3;
                    return response;
                }

                // If all returned 0, log/diagnose
                string summaryLog = string.Join(" | ", diagnosticLogs);
                logger.Warn("Failed to retrieve counters for province {0}. Diagnostics: {1}", provCode, summaryLog);

                if (!anySuccess)
                {
                    response.ErrorMessage = summaryLog;
                }
                else
                {
                    response.ErrorMessage = string.Empty;
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching counters for province {0}", provCode);
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private List<CounterData> GetCountersFromConnection(string connectionName, string provCode, out string logMessage, out bool success)
        {
            var counters = new List<CounterData>();
            logMessage = "";
            success = false;

            try
            {
                var connectionSetting = ConfigurationManager.ConnectionStrings[connectionName];
                if (connectionSetting == null || string.IsNullOrWhiteSpace(connectionSetting.ConnectionString))
                {
                    logMessage = $"{connectionName} connection string is missing or empty in configuration.";
                    return counters;
                }

                using (var conn = new OleDbConnection(connectionSetting.ConnectionString))
                {
                    conn.Open();

                    if (provCode == "1" || provCode == "7" || provCode == "9" || provCode == "C")
                    {
                        const string sqlSpecial = @"
                            SELECT a.count_no
                            FROM cus_counts a, agent_info b
                            WHERE a.agent = b.agent_code
                              AND b.prov_code = ?
                            ORDER BY a.count_no";

                        using (var cmd = new OleDbCommand(sqlSpecial, conn))
                        {
                            cmd.Parameters.AddWithValue("@provCode", provCode);
                            cmd.CommandTimeout = 15;

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var countNo = GetStringValue(reader, "count_no");
                                    counters.Add(new CounterData
                                    {
                                        CounterNo = countNo,
                                        CounterName = countNo
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        const string sqlGeneral = @"
                            SELECT count_no
                            FROM cus_counts
                            ORDER BY count_no";

                        using (var cmd = new OleDbCommand(sqlGeneral, conn))
                        {
                            cmd.CommandTimeout = 15;

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var countNo = GetStringValue(reader, "count_no");
                                    counters.Add(new CounterData
                                    {
                                        CounterNo = countNo,
                                        CounterName = countNo
                                    });
                                }
                            }
                        }
                    }
                    logMessage = $"Success on {connectionName}: {counters.Count} counters found.";
                    success = true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("-111") || ex.Message.Contains("ISAM error"))
                {
                    logMessage = $"Success on {connectionName} (0 counters found).";
                    success = true;
                }
                else
                {
                    logMessage = $"Error on {connectionName}: {ex.Message}";
                    success = false;
                }
            }

            return counters;
        }

        public POSCounterCollectionResponse GetPOSCounterCollectionBreakup(POSCounterCollectionRequest request)
        {
            var response = new POSCounterCollectionResponse
            {
                Records = new List<POSCounterCollectionRecord>(),
                ErrorMessage = string.Empty
            };

            try
            {
                if (request == null)
                {
                    response.ErrorMessage = "Request body is required.";
                    return response;
                }

                var transDate = NormalizeDateString(request.TransDate);

                if (string.IsNullOrWhiteSpace(transDate))
                {
                    response.ErrorMessage = "Transaction date is required.";
                    return response;
                }

                response.TransDate = transDate;
                response.Province = request.ProvCode ?? "*";
                response.Area = request.AreaCode ?? "*";
                response.Counter = request.CounterNo ?? "*";

                var diagnosticLogs = new List<string>();
                bool anySuccess = false;

                // 1. Try PmntConnectionName (InformixPmntConsld)
                string log1;
                var list1 = GetCollectionBreakupFromConnection(PmntConnectionName, request, transDate, out log1, out bool success1);
                diagnosticLogs.Add(log1);
                if (success1) anySuccess = true;
                if (list1 != null && list1.Count > 0)
                {
                    response.Records = list1;
                    response.RecordCount = response.Records.Count;
                    response.TotalAmount = response.Records.Sum(record => GetDecimalValue(record.TransAmount));
                    return response;
                }

                // 2. Try InformixPosPayment
                string log2;
                var list2 = GetCollectionBreakupFromConnection("InformixPosPayment", request, transDate, out log2, out bool success2);
                diagnosticLogs.Add(log2);
                if (success2) anySuccess = true;
                if (list2 != null && list2.Count > 0)
                {
                    response.Records = list2;
                    response.RecordCount = response.Records.Count;
                    response.TotalAmount = response.Records.Sum(record => GetDecimalValue(record.TransAmount));
                    return response;
                }

                // 3. Try BillsmryConnectionName (InformixConnection)
                string log3;
                var list3 = GetCollectionBreakupFromConnection(BillsmryConnectionName, request, transDate, out log3, out bool success3);
                diagnosticLogs.Add(log3);
                if (success3) anySuccess = true;
                if (list3 != null && list3.Count > 0)
                {
                    response.Records = list3;
                    response.RecordCount = response.Records.Count;
                    response.TotalAmount = response.Records.Sum(record => GetDecimalValue(record.TransAmount));
                    return response;
                }

                // If all returned 0 records, return diagnostic info only if there was a real system failure
                string summaryLog = string.Join(" | ", diagnosticLogs);
                logger.Warn("Failed to retrieve POS counter collection breakup. Diagnostics: {0}", summaryLog);

                if (!anySuccess)
                {
                    response.ErrorMessage = summaryLog;
                }
                else
                {
                    response.ErrorMessage = string.Empty;
                }

                response.RecordCount = 0;
                response.TotalAmount = 0m;
                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching POS counter collection breakup");
                response.ErrorMessage = ex.Message;
                return response;
            }
        }

        private List<POSCounterCollectionRecord> GetCollectionBreakupFromConnection(string connectionName, POSCounterCollectionRequest request, string transDate, out string logMessage, out bool success)
        {
            var records = new List<POSCounterCollectionRecord>();
            logMessage = "";
            success = false;

            try
            {
                var connectionSetting = ConfigurationManager.ConnectionStrings[connectionName];
                if (connectionSetting == null || string.IsNullOrWhiteSpace(connectionSetting.ConnectionString))
                {
                    logMessage = $"{connectionName} connection string is missing or empty in configuration.";
                    return records;
                }

                using (var conn = new OleDbConnection(connectionSetting.ConnectionString))
                {
                    conn.Open();

                    var sql = @"
                        SELECT acc_no,
                               count_no,
                               stub_no,
                               trans_amt,
                               pay_mode,
                               trans_type,
                               piv_no,
                               area_code
                        FROM cus_tran
                        WHERE trans_date = ? AND trans_type = '0'";

                    var parameters = new List<OleDbParameter>
                    {
                        new OleDbParameter("@transDate", transDate)
                    };

                    // Build dynamic WHERE clause based on filters
                    if (!string.IsNullOrWhiteSpace(request.AreaCode) && request.AreaCode != "*")
                    {
                        sql += " AND area_code = ?";
                        parameters.Add(new OleDbParameter("@areaCode", request.AreaCode));
                    }

                    if (!string.IsNullOrWhiteSpace(request.CounterNo) && request.CounterNo != "*")
                    {
                        sql += " AND count_no = ?";
                        parameters.Add(new OleDbParameter("@countNo", request.CounterNo));
                    }

                    if (!string.IsNullOrWhiteSpace(request.PayMode) && request.PayMode != "*")
                    {
                        sql += " AND pay_mode = ?";
                        parameters.Add(new OleDbParameter("@payMode", request.PayMode));
                    }

                    if (!string.IsNullOrWhiteSpace(request.PayType) && request.PayType != "*")
                    {
                        string dbPayType = request.PayType;
                        if (dbPayType.Equals("Bill", StringComparison.OrdinalIgnoreCase))
                        {
                            dbPayType = "B";
                        }
                        else if (dbPayType.Equals("PIV", StringComparison.OrdinalIgnoreCase))
                        {
                            dbPayType = "P";
                        }

                        sql += " AND pay_type = ?";
                        parameters.Add(new OleDbParameter("@payType", dbPayType));
                    }

                    sql += " ORDER BY count_no, stub_no";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 15;
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.Add(param);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var countNo = GetStringValue(reader, "count_no");
                                var payMode = GetStringValue(reader, "pay_mode");
                                var counterName = GetCounterDetails(countNo);
                                var payModeDescription = GetCodeDescription(payMode);

                                records.Add(new POSCounterCollectionRecord
                                {
                                    AccountNo = GetStringValue(reader, "acc_no"),
                                    CounterNo = countNo,
                                    StubNo = GetStringValue(reader, "stub_no"),
                                    TransAmount = GetStringValue(reader, "trans_amt"),
                                    PayMode = payMode,
                                    TransType = GetStringValue(reader, "trans_type"),
                                    PIVNo = GetStringValue(reader, "piv_no"),
                                    AreaCode = GetStringValue(reader, "area_code"),
                                    CounterName = counterName,
                                    PayModeDescription = payModeDescription
                                });
                            }
                        }
                    }
                    logMessage = $"Success on {connectionName}: {records.Count} records found.";
                    success = true;
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("-111") || ex.Message.Contains("ISAM error"))
                {
                    logMessage = $"Success on {connectionName} (0 records found).";
                    success = true;
                }
                else
                {
                    logMessage = $"Error on {connectionName}: {ex.Message}";
                    success = false;
                }
            }

            return records;
        }

        public PayModeListResponse GetPayModes()
        {
            var response = new PayModeListResponse
            {
                PayModes = new List<PayModeData>(),
                ErrorMessage = string.Empty
            };

            try
            {
                using (var conn = GetConnection(PmntConnectionName))
                {
                    conn.Open();
                    const string sql = @"
                        SELECT DISTINCT pay_mode, code_discrp
                        FROM code_paymode
                        WHERE pay_mode IS NOT NULL
                        ORDER BY code_discrp";

                    using (var cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.CommandTimeout = 30;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                response.PayModes.Add(new PayModeData
                                {
                                    PayMode = GetStringValue(reader, "pay_mode"),
                                    CodeDescription = GetStringValue(reader, "code_discrp")
                                });
                            }
                        }
                    }
                }

                // If no records found, add fallback values
                if (response.PayModes.Count == 0)
                {
                    response.PayModes.Add(new PayModeData { PayMode = "C", CodeDescription = "Cash" });
                    response.PayModes.Add(new PayModeData { PayMode = "Q", CodeDescription = "Cheque" });
                    response.PayModes.Add(new PayModeData { PayMode = "O", CodeDescription = "Online" });
                }

                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error while fetching pay modes");
                response.ErrorMessage = ex.Message;
                // Add fallback values in case of DB failure
                response.PayModes.Add(new PayModeData { PayMode = "C", CodeDescription = "Cash" });
                response.PayModes.Add(new PayModeData { PayMode = "Q", CodeDescription = "Cheque" });
                response.PayModes.Add(new PayModeData { PayMode = "O", CodeDescription = "Online" });
                return response;
            }
        }

        public BillTypeListResponse GetBillTypes()
        {
            var response = new BillTypeListResponse
            {
                BillTypes = new List<BillTypeData>
                {
                    new BillTypeData { BillType = "Bill" },
                    new BillTypeData { BillType = "PIV" }
                },
                ErrorMessage = string.Empty
            };

            return response;
        }

        #endregion
    }
}
