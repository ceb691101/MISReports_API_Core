using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAO.SolarInformation.SolarCustomerInformation
{
    public class SolarCustomerInforBulkDao
    {
        private readonly DBConnection _dbConnection;

        public SolarCustomerInforBulkDao()
        {
            _dbConnection = new DBConnection();
        }

        // Main method to get complete customer information
        public SolarCustomerInforResponse GetCustomerInformation(string accountNumber)
        {
            var response = new SolarCustomerInforResponse
            {
                CustomerType = "Bulk",
                AccountNumber = accountNumber
            };

            try
            {
                System.Diagnostics.Trace.WriteLine($"=== START Bulk Customer Info for {accountNumber} ===");

                // Step 1: Get the latest bill cycle from netmtcons
                string latestBillCycle = GetLatestBillCycle(accountNumber);

                System.Diagnostics.Trace.WriteLine($"Latest bill cycle from netmtcons: {latestBillCycle ?? "NULL"}");

                if (string.IsNullOrEmpty(latestBillCycle))
                {
                    response.ErrorMessage = "No bill cycle found for this account number";
                    return response;
                }

                // Step 2: Get customer basic information
                response.CustomerInfo = GetCustomerBasicInfo(accountNumber, latestBillCycle);

                

                // Step 3: Get last 6 months energy history
                response.EnergyHistory = GetEnergyHistory(accountNumber, latestBillCycle);
                

                System.Diagnostics.Trace.WriteLine($"=== END Bulk Customer Info - Success ===");
            }
            catch (Exception ex)
            {
                response.ErrorMessage = $"Error retrieving customer information: {ex.Message}";
                System.Diagnostics.Trace.WriteLine($"ERROR in GetCustomerInformation: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return response;
        }

        // Get latest bill cycle from netmtcons
        private string GetLatestBillCycle(string accountNumber)
        {
            string billCycle = null;

            try
            {
                using (var conn = _dbConnection.GetConnection(true)) // Use BULK connection
                {
                    conn.Open();

                    string query = "SELECT MAX(bill_cycle) FROM netmtcons WHERE acc_nbr = ?";

                    using (var cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@acc_nbr", OleDbType.VarChar).Value = accountNumber;

                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            billCycle = result.ToString().Trim();
                            System.Diagnostics.Trace.WriteLine($"Latest bill cycle: {billCycle}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in GetLatestBillCycle: {ex.Message}");
                throw;
            }

            return billCycle;
        }

        // Get customer basic information
        private SolarCustomerBasicInfo GetCustomerBasicInfo(string accountNumber, string billCycle)
        {
            SolarCustomerBasicInfo customerInfo = null;

            try
            {
                using (var conn = _dbConnection.GetConnection(true)) // Use BULK connection
                {
                    conn.Open();

                    // Query 1: Get customer details from customer table
                    string query1 = @"SELECT name, address_l1, address_l2, city, tel_nbr, 
                             net_type, cntr_dmnd, tariff, area_cd
                             FROM customer 
                             WHERE acc_nbr = ?";

                    using (var cmd = new OleDbCommand(query1, conn))
                    {
                        cmd.Parameters.Add("@acc_nbr", OleDbType.VarChar).Value = accountNumber;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customerInfo = new SolarCustomerBasicInfo
                                {
                                    AccountNumber = accountNumber,
                                    AreaCode = reader["area_cd"]?.ToString()?.Trim() ?? "",
                                    Name = reader["name"]?.ToString()?.Trim() ?? "",
                                    Address1 = reader["address_l1"]?.ToString()?.Trim() ?? "",
                                    Address2 = reader["address_l2"]?.ToString()?.Trim() ?? "",
                                    Address3 = reader["city"]?.ToString()?.Trim() ?? "",
                                    TelephoneNumber = reader["tel_nbr"]?.ToString()?.Trim() ?? "",
                                    NetType = reader["net_type"]?.ToString()?.Trim() ?? "",
                                    TariffCode = reader["tariff"]?.ToString()?.Trim() ?? "",
                                    // Initialize area properties
                                    AreaName = "",
                                    ProvinceName = "",
                                    Region = ""
                                };

                                // Contract Demand (specific to bulk customers)
                                if (decimal.TryParse(reader["cntr_dmnd"]?.ToString(), out decimal contractDemand))
                                {
                                    customerInfo.ContractDemand = ((int)contractDemand).ToString();
                                }
                                else
                                {
                                    customerInfo.ContractDemand = "";
                                }

                                // Construct full address
                                var addressParts = new List<string>();
                                if (!string.IsNullOrEmpty(customerInfo.Address1)) addressParts.Add(customerInfo.Address1);
                                if (!string.IsNullOrEmpty(customerInfo.Address2)) addressParts.Add(customerInfo.Address2);
                                if (!string.IsNullOrEmpty(customerInfo.Address3)) addressParts.Add(customerInfo.Address3);
                                customerInfo.Address = string.Join(", ", addressParts);

                                System.Diagnostics.Trace.WriteLine($"Customer data retrieved from customer table");
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"No customer data found in customer table");
                                customerInfo = new SolarCustomerBasicInfo
                                {
                                    AccountNumber = accountNumber,
                                    AreaName = "",
                                    ProvinceName = "",
                                    Region = ""
                                };
                            }
                        }
                    }

                    // Query 2: Get area information
                    if (customerInfo != null && !string.IsNullOrEmpty(customerInfo.AreaCode))
                    {
                        System.Diagnostics.Trace.WriteLine($"=== Attempting to fetch area info for AreaCode: '{customerInfo.AreaCode}' ===");

                        string areaQuery = @"SELECT a.area_name, p.prov_name, p.prov_code, a.region 
                            FROM areas a, provinces p 
                            WHERE a.prov_code = p.prov_code 
                            AND a.area_code = ?";

                        using (var cmd = new OleDbCommand(areaQuery, conn))
                        {
                            cmd.Parameters.Add("@area_code", OleDbType.VarChar).Value = customerInfo.AreaCode;

                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    customerInfo.AreaName = reader["area_name"]?.ToString()?.Trim() ?? "";
                                    customerInfo.ProvinceCode = reader["prov_code"]?.ToString()?.Trim() ?? "";
                                    customerInfo.ProvinceName = reader["prov_name"]?.ToString()?.Trim() ?? "";
                                    customerInfo.Region = reader["region"]?.ToString()?.Trim() ?? "";

                                    System.Diagnostics.Trace.WriteLine($"Area information retrieved successfully:");
                                }
                                else
                                {
                                    System.Diagnostics.Trace.WriteLine($"No area information found for area_cd: '{customerInfo.AreaCode}'");
                                    customerInfo.AreaName = customerInfo.AreaCode; // Fallback to area code
                                    customerInfo.ProvinceCode = "";
                                    customerInfo.ProvinceName = " ";
                                    customerInfo.Region = " ";
                                }
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"✗ Skipping area query - CustomerInfo null: {customerInfo == null}, AreaCode empty: {string.IsNullOrEmpty(customerInfo?.AreaCode)}");
                    }

                    // Query 3: Get tariff and additional info from netmtcons
                    string query2 = @"SELECT rate, gen_cap 
                             FROM netmtcons 
                             WHERE acc_nbr = ? AND bill_cycle = ?";

                    using (var cmd = new OleDbCommand(query2, conn))
                    {
                        cmd.Parameters.Add("@acc_nbr", OleDbType.VarChar).Value = accountNumber;
                        cmd.Parameters.Add("@bill_cycle", OleDbType.Integer).Value = int.Parse(billCycle);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (decimal.TryParse(reader["gen_cap"]?.ToString(), out decimal genCap))
                                {
                                    customerInfo.GenerationCapacity = genCap;
                                }

                                System.Diagnostics.Trace.WriteLine($"Tariff and rate retrieved from netmtcons");
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"WARNING: No netmtcons data for bill_cycle {billCycle}");
                            }
                        }
                    }

                    // Query 4: Get agreement date from netmeter table
                    string query3 = "SELECT agrmnt_date FROM netmeter WHERE acc_nbr = ?";

                    using (var cmd = new OleDbCommand(query3, conn))
                    {
                        cmd.Parameters.Add("@acc_nbr", OleDbType.VarChar).Value = accountNumber;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customerInfo.AgreementDate = reader["agrmnt_date"]?.ToString()?.Trim() ?? "";
                                System.Diagnostics.Trace.WriteLine($"Agreement date retrieved from netmeter");
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"No agreement date found in netmeter");
                            }
                        }
                    }

                    customerInfo.Bank = "";
                    customerInfo.Branch = "";
                    customerInfo.BankAccountNumber = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in GetCustomerBasicInfo: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }

            return customerInfo;
        }

        // Get last 6 months energy history
        // Updated GetEnergyHistory method with reading dates and export readings
        private List<SolarEnergyHistoryModel> GetEnergyHistory(string accountNumber, string latestBillCycle)
        {
            var energyHistory = new List<SolarEnergyHistoryModel>();

            try
            {
                using (var conn = _dbConnection.GetConnection(true)) // Use BULK connection
                {
                    conn.Open();

                    // Calculate the bill cycle range for last 6 months
                    string startCycle = CalculateStartCycle(latestBillCycle, 6);

                    // Updated query: Get energy data from netmtcons and reading details from rdngs
                    string query = @"SELECT 
                                m.bill_cycle, 
                                m.exp_kwd_units, 
                                m.imp_kwd_units, 
                                m.rate, 
                                m.unitsale,
                                r.mtr_nbr,
                                r.rdng_date,
                                r.prv_date,
                                r.rdn,
                                r.prv_rdn,
                                mt.tot_untskwo,
                                mt.tot_untskwd,
                                mt.tot_untskwp,
                                mt.tot_kva
                           FROM netmtcons m
                           LEFT JOIN rdngs r ON m.acc_nbr = r.acc_nbr 
                                AND r.added_blcy = m.bill_cycle 
                                AND r.mtr_type = 'KWD' 
                                AND r.mtr_seq = '2'
                           LEFT JOIN mon_tot mt ON m.acc_nbr = mt.acc_nbr 
                                AND m.bill_cycle = mt.bill_cycle
                           WHERE m.bill_cycle BETWEEN ? AND ? 
                                AND m.acc_nbr = ? 
                           ORDER BY m.bill_cycle";

                    using (var cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@start_cycle", OleDbType.Integer).Value = int.Parse(startCycle);
                        cmd.Parameters.Add("@end_cycle", OleDbType.Integer).Value = int.Parse(latestBillCycle);
                        cmd.Parameters.Add("@acc_nbr", OleDbType.VarChar).Value = accountNumber;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var energyData = new SolarEnergyHistoryModel
                                {
                                    CalcCycle = reader["bill_cycle"]?.ToString()?.Trim() ?? ""
                                };

                                // Parse imp_kwd_units (Energy Imported)
                                string imported = reader["imp_kwd_units"]?.ToString()?.Trim() ?? "";
                                if (decimal.TryParse(imported, out decimal impValue))
                                {
                                    energyData.EnergyImported = impValue % 1 == 0
                                        ? ((int)impValue).ToString()
                                        : impValue.ToString();
                                }
                                else
                                {
                                    energyData.EnergyImported = "";
                                }

                                // Parse exp_kwd_units (Energy Exported)
                                string exported = reader["exp_kwd_units"]?.ToString()?.Trim() ?? "";
                                if (decimal.TryParse(exported, out decimal expValue))
                                {
                                    energyData.EnergyExported = expValue % 1 == 0
                                        ? ((int)expValue).ToString()
                                        : expValue.ToString();
                                }
                                else
                                {
                                    energyData.EnergyExported = "";
                                }

                                // Parse rate (Unit Cost)
                                if (decimal.TryParse(reader["rate"]?.ToString(), out decimal rate))
                                {
                                    energyData.UnitCost = rate;
                                }

                                // Parse unitsale (Net Units)
                                if (decimal.TryParse(reader["unitsale"]?.ToString(), out decimal unitSale))
                                {
                                    energyData.UnitSale = unitSale;
                                }

                                // NEW: Get meter number from the joined rdngs table
                                energyData.MeterNumber = reader["mtr_nbr"]?.ToString()?.Trim() ?? "";

                                // NEW: Get present reading date
                                energyData.PresentReadingDate = reader["rdng_date"]?.ToString()?.Trim() ?? "";

                                // NEW: Get previous reading date
                                energyData.PreviousReadingDate = reader["prv_date"]?.ToString()?.Trim() ?? "";

                                // NEW: Get present reading export (rdn)
                                string presentRdExport = reader["rdn"]?.ToString()?.Trim() ?? "";
                                if (decimal.TryParse(presentRdExport, out decimal presentRdnValue))
                                {
                                    energyData.PresentReadingExport = presentRdnValue % 1 == 0
                                        ? ((int)presentRdnValue).ToString()
                                        : presentRdnValue.ToString();
                                }
                                else
                                {
                                    energyData.PresentReadingExport = "";
                                }

                                // NEW: Get previous reading export (prv_rdn)
                                string previousRdExport = reader["prv_rdn"]?.ToString()?.Trim() ?? "";
                                if (decimal.TryParse(previousRdExport, out decimal prevRdnValue))
                                {
                                    energyData.PreviousReadingExport = prevRdnValue % 1 == 0
                                        ? ((int)prevRdnValue).ToString()
                                        : prevRdnValue.ToString();
                                }
                                else
                                {
                                    energyData.PreviousReadingExport = "";
                                }

                                // Get monthly total fields from mon_tot table
                                // Kwo (tot_untskwo)
                                string kwo = reader["tot_untskwo"]?.ToString()?.Trim() ?? "";
                                if (decimal.TryParse(kwo, out decimal kwoValue))
                                {
                                    energyData.Kwo = kwoValue % 1 == 0
                                        ? ((int)kwoValue).ToString()
                                        : kwoValue.ToString();
                                }
                                else
                                {
                                    energyData.Kwo = "";
                                }

                                // Kwd (tot_untskwd)
                                string kwd = reader["tot_untskwd"]?.ToString()?.Trim() ?? "";
                                if (decimal.TryParse(kwd, out decimal kwdValue))
                                {
                                    energyData.Kwd = kwdValue % 1 == 0
                                        ? ((int)kwdValue).ToString()
                                        : kwdValue.ToString();
                                }
                                else
                                {
                                    energyData.Kwd = "";
                                }

                                // Kwp (tot_untskwp)
                                string kwp = reader["tot_untskwp"]?.ToString()?.Trim() ?? "";
                                if (decimal.TryParse(kwp, out decimal kwpValue))
                                {
                                    energyData.Kwp = kwpValue % 1 == 0
                                        ? ((int)kwpValue).ToString()
                                        : kwpValue.ToString();
                                }
                                else
                                {
                                    energyData.Kwp = "";
                                }

                                // Kva (tot_kva)
                                string kva = reader["tot_kva"]?.ToString()?.Trim() ?? "";
                                if (decimal.TryParse(kva, out decimal kvaValue))
                                {
                                    energyData.Kva = kvaValue % 1 == 0
                                        ? ((int)kvaValue).ToString()
                                        : kvaValue.ToString();
                                }
                                else
                                {
                                    energyData.Kva = "";
                                }

                                energyHistory.Add(energyData);
                            }
                        }
                    }

                    System.Diagnostics.Trace.WriteLine($"Retrieved {energyHistory.Count} energy history records with reading details");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in GetEnergyHistory: {ex.Message}");
                throw;
            }

            return energyHistory;
        }

        // Helper method to calculate start cycle for last N months
        private string CalculateStartCycle(string latestCycle, int monthsBack)
        {
            try
            {
                // Bill cycle is a sequential number
                int cycle = int.Parse(latestCycle);
                int startCycle = cycle - monthsBack;

                System.Diagnostics.Trace.WriteLine($"Calculated start cycle: {startCycle} (from {latestCycle}, {monthsBack} cycles back)");

                return startCycle.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in CalculateStartCycle: {ex.Message}");
                return latestCycle;
            }
        }

        // Check if account exists in bulk database
        public bool AccountExists(string accountNumber)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(true)) // Use BULK connection
                {
                    conn.Open();

                    string query = "SELECT COUNT(*) FROM netmtcons WHERE acc_nbr = ?";

                    using (var cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@acc_nbr", OleDbType.VarChar).Value = accountNumber;

                        var result = cmd.ExecuteScalar();
                        int count = Convert.ToInt32(result);

                        System.Diagnostics.Trace.WriteLine($"Account {accountNumber} exists in bulk database: {count > 0}");
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"ERROR in AccountExists: {ex.Message}");
                return false;
            }
        }
    }
}