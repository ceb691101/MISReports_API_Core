using MISReports_Api.DBAccess;
using MISReports_Api.Models.SolarInformation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace MISReports_Api.DAO.SolarInformation.SolarCustomerInformation
{
    public class SolarCustomerInforOrdinaryDao
    {
        private readonly DBConnection _dbConnection;

        public SolarCustomerInforOrdinaryDao()
        {
            _dbConnection = new DBConnection();
        }

        // Main method to get complete customer information
        public SolarCustomerInforResponse GetCustomerInformation(string accountNumber)
        {
            var response = new SolarCustomerInforResponse
            {
                CustomerType = "Ordinary",
                AccountNumber = accountNumber
            };

            try
            {
                // Step 1: Get the latest bill cycle from netmtcons (this is the "current" cycle)
                string latestBillCycle = GetLatestBillCycle(accountNumber);

                System.Diagnostics.Trace.WriteLine($"Latest bill cycle from netmtcons: {latestBillCycle ?? "NULL"}");

                if (string.IsNullOrEmpty(latestBillCycle))
                {
                    response.ErrorMessage = "No bill cycle found for this account number";
                    return response;
                }

                // Step 2: Find the closest available bill_cycle in prn_dat_1 
                // (it might be the same or slightly older)
                string availableCycleInPrnDat = GetClosestAvailableCycleInPrnDat(accountNumber, latestBillCycle);

                System.Diagnostics.Trace.WriteLine($"Available cycle in prn_dat_1: {availableCycleInPrnDat ?? "NULL"}");

                if (string.IsNullOrEmpty(availableCycleInPrnDat))
                {
                    response.ErrorMessage = "No customer data found in prn_dat_1 for this account";
                    return response;
                }

                // Step 3: Get customer basic information using the available cycle
                response.CustomerInfo = GetCustomerBasicInfo(accountNumber, availableCycleInPrnDat);

                if (response.CustomerInfo == null)
                {
                    response.ErrorMessage = "Customer information not found";
                    return response;
                }

                // Step 4: Get area information (area name, province, region)
                if (!string.IsNullOrEmpty(response.CustomerInfo.AreaCode))
                {
                    var areaInfo = GetAreaInformation(response.CustomerInfo.AreaCode);
                    if (areaInfo != null)
                    {
                        response.CustomerInfo.AreaName = areaInfo.AreaName;
                        response.CustomerInfo.ProvinceCode = areaInfo.ProvinceCode;
                        response.CustomerInfo.ProvinceName = areaInfo.ProvinceName;
                        response.CustomerInfo.Region = areaInfo.Region;
                    }
                }

                // Step 5: Get last 6 months energy history using the latest cycle from netmtcons
                response.EnergyHistory = GetEnergyHistory(accountNumber, latestBillCycle, response.CustomerInfo);

                System.Diagnostics.Trace.WriteLine($"Successfully retrieved customer information for account: {accountNumber}");
            }
            catch (Exception ex)
            {
                response.ErrorMessage = $"Error retrieving customer information: {ex.Message}";
                System.Diagnostics.Trace.WriteLine($"Error in GetCustomerInformation: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            return response;
        }

        // Query 1: Get latest bill cycle from netmtcons
        private string GetLatestBillCycle(string accountNumber)
        {
            string billCycle = null;

            try
            {
                using (var conn = _dbConnection.GetConnection(false)) // Use ordinary connection
                {
                    conn.Open();

                    string query = "SELECT MAX(bill_cycle) FROM netmtcons WHERE acct_number = ?";

                    using (var cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@acct_number", OleDbType.VarChar).Value = accountNumber;

                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            billCycle = result.ToString().Trim();
                            System.Diagnostics.Trace.WriteLine($"Latest bill cycle for {accountNumber}: {billCycle}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetLatestBillCycle: {ex.Message}");
                throw;
            }

            return billCycle;
        }

        // NEW: Find the closest available cycle in prn_dat_1 (same or closest older cycle)
        private string GetClosestAvailableCycleInPrnDat(string accountNumber, string targetCycle)
        {
            string availableCycle = null;

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    // Try to find the target cycle first, if not found, get the closest one that's <= target
                    string query = @"SELECT MAX(bill_cycle) 
                                   FROM prn_dat_1 
                                   WHERE acct_number = ? 
                                   AND bill_cycle <= ?";

                    using (var cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@acct_number", OleDbType.VarChar).Value = accountNumber;
                        cmd.Parameters.Add("@bill_cycle", OleDbType.Integer).Value = int.Parse(targetCycle);

                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            availableCycle = result.ToString().Trim();

                            if (availableCycle == targetCycle)
                            {
                                System.Diagnostics.Trace.WriteLine($"Found exact match in prn_dat_1: cycle {availableCycle}");
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"Using closest available cycle in prn_dat_1: {availableCycle} (target was {targetCycle})");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Trace.WriteLine($"No cycles found in prn_dat_1 for account {accountNumber}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetClosestAvailableCycleInPrnDat: {ex.Message}");
                throw;
            }

            return availableCycle;
        }

        // Query 2 & 3: Get customer basic information
        private SolarCustomerBasicInfo GetCustomerBasicInfo(string accountNumber, string billCycle)
        {
            SolarCustomerBasicInfo customerInfo = null;

            try
            {
                using (var conn = _dbConnection.GetConnection(false)) // Use ordinary connection
                {
                    conn.Open();

                    // Query 2: Get customer details from prn_dat_1
                    string query1 = @"SELECT cust_fname, cust_lname, address_1, address_2, address_3, 
                                     no_of_phase, met_no1, met_no2, met_no3, tele_nol, crnt_depot, substn_code 
                                     FROM prn_dat_1 
                                     WHERE bill_cycle = ? AND acct_number = ?";

                    using (var cmd = new OleDbCommand(query1, conn))
                    {
                        cmd.Parameters.Add("@bill_cycle", OleDbType.Integer).Value = int.Parse(billCycle);
                        cmd.Parameters.Add("@acct_number", OleDbType.VarChar).Value = accountNumber;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customerInfo = new SolarCustomerBasicInfo
                                {
                                    AccountNumber = accountNumber,
                                    FirstName = reader["cust_fname"]?.ToString()?.Trim() ?? "",
                                    LastName = reader["cust_lname"]?.ToString()?.Trim() ?? "",
                                    Address1 = reader["address_1"]?.ToString()?.Trim() ?? "",
                                    Address2 = reader["address_2"]?.ToString()?.Trim() ?? "",
                                    Address3 = reader["address_3"]?.ToString()?.Trim() ?? "",
                                    NoOfPhase = reader["no_of_phase"]?.ToString()?.Trim() ?? "",
                                    MeterNumber1 = reader["met_no1"]?.ToString()?.Trim() ?? "",
                                    MeterNumber2 = reader["met_no2"]?.ToString()?.Trim() ?? "",
                                    MeterNumber3 = reader["met_no3"]?.ToString()?.Trim() ?? "",
                                    TelephoneNumber = reader["tele_nol"]?.ToString()?.Trim() ?? "",
                                    CrntDepot = reader["crnt_depot"]?.ToString()?.Trim() ?? "",
                                    SubstnCode = reader["substn_code"]?.ToString()?.Trim() ?? "",
                                };

                                // Construct full name
                                customerInfo.Name = $"{customerInfo.FirstName} {customerInfo.LastName}".Trim();

                                // Construct full address
                                var addressParts = new List<string>();
                                if (!string.IsNullOrEmpty(customerInfo.Address1)) addressParts.Add(customerInfo.Address1);
                                if (!string.IsNullOrEmpty(customerInfo.Address2)) addressParts.Add(customerInfo.Address2);
                                if (!string.IsNullOrEmpty(customerInfo.Address3)) addressParts.Add(customerInfo.Address3);
                                customerInfo.Address = string.Join(", ", addressParts);

                                System.Diagnostics.Trace.WriteLine($"Customer data retrieved from prn_dat_1 for cycle {billCycle}");
                            }
                        }
                    }
                    // If customer info not found, return null
                    if (customerInfo == null)
                    {
                        System.Diagnostics.Trace.WriteLine($"No customer data in prn_dat_1 for cycle {billCycle}");
                        return null;
                    }
                 
                    // Use the same bill_cycle for consistency
                    string query2 = @"SELECT area_code, tariff_code, net_type, bank_code, bran_code, 
                                     bk_ac_no, rate, agrmnt_date, gen_cap
                                     FROM netmtcons 
                                     WHERE calc_cycle = ? AND acct_number = ?";

                    using (var cmd = new OleDbCommand(query2, conn))
                    {
                        cmd.Parameters.Add("@calc_cycle", OleDbType.Integer).Value = int.Parse(billCycle);
                        cmd.Parameters.Add("@acct_number", OleDbType.VarChar).Value = accountNumber;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                customerInfo.AreaCode = reader["area_code"]?.ToString()?.Trim() ?? "";
                                customerInfo.TariffCode = reader["tariff_code"]?.ToString()?.Trim() ?? "";
                                customerInfo.NetType = reader["net_type"]?.ToString()?.Trim() ?? "";
                                customerInfo.BankCode = reader["bank_code"]?.ToString()?.Trim() ?? "";
                                customerInfo.BranchCode = reader["bran_code"]?.ToString()?.Trim() ?? "";
                                customerInfo.BankAccountNumber = reader["bk_ac_no"]?.ToString()?.Trim() ?? "";

                                // Parse agreement date
                                customerInfo.AgreementDate = reader["agrmnt_date"]?.ToString()?.Trim() ?? "";

                                //Parse gen_cap (Generation Capacity)
                                if (decimal.TryParse(reader["gen_cap"]?.ToString(), out decimal genCap))
                                {
                                    customerInfo.GenerationCapacity = genCap;
                                }

                                // Set bank and branch
                                customerInfo.Bank = customerInfo.BankCode;
                                customerInfo.Branch = customerInfo.BranchCode;

                                System.Diagnostics.Trace.WriteLine($"Additional data retrieved from netmtcons for cycle {billCycle}");
                            }
                            else
                            {
                                System.Diagnostics.Trace.WriteLine($"WARNING: No netmtcons data for calc_cycle={billCycle}");
                                // Continue anyway - we have customer basic info from prn_dat_1
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetCustomerBasicInfo: {ex.Message}");
                throw;
            }

            return customerInfo;
        }

        // NEW: Get area information (area name, province, region)
        private AreaInformation GetAreaInformation(string areaCode)
        {
            AreaInformation areaInfo = null;

            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string query = @"SELECT a.area_name, p.prov_name, p.prov_code, a.region 
                                   FROM areas a, provinces p 
                                   WHERE a.prov_code = p.prov_code 
                                   AND a.area_code = ?";

                    using (var cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@area_code", OleDbType.VarChar).Value = areaCode;

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                areaInfo = new AreaInformation
                                {
                                    AreaName = reader["area_name"]?.ToString()?.Trim() ?? "",
                                    ProvinceCode = reader["prov_code"]?.ToString()?.Trim() ?? "",
                                    ProvinceName = reader["prov_name"]?.ToString()?.Trim() ?? "",
                                    Region = reader["region"]?.ToString()?.Trim() ?? ""
                                };

                                System.Diagnostics.Trace.WriteLine($"Area information retrieved for area code: {areaCode}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetAreaInformation: {ex.Message}");
                // Don't throw - area info is supplementary
            }

            return areaInfo;
        }

        // Query 4: Get last 6 months energy history
        private List<SolarEnergyHistoryModel> GetEnergyHistory(string accountNumber, string latestBillCycle, SolarCustomerBasicInfo customerInfo)
        {
            var energyHistory = new List<SolarEnergyHistoryModel>();

            try
            {
                using (var conn = _dbConnection.GetConnection(false)) // Use ordinary connection
                {
                    conn.Open();

                    // Calculate the bill cycle range for last 6 months
                    string startCycle = CalculateStartCycle(latestBillCycle, 6);

                    string query = @"SELECT calc_cycle, units_in, units_out, unitsale, rate 
                                   FROM netmtcons 
                                   WHERE calc_cycle BETWEEN ? AND ? AND acct_number = ? 
                                   ORDER BY calc_cycle";

                    using (var cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@start_cycle", OleDbType.Integer).Value = int.Parse(startCycle);
                        cmd.Parameters.Add("@end_cycle", OleDbType.Integer).Value = int.Parse(latestBillCycle);
                        cmd.Parameters.Add("@acct_number", OleDbType.VarChar).Value = accountNumber;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var energyData = new SolarEnergyHistoryModel
                                {
                                    CalcCycle = reader["calc_cycle"]?.ToString()?.Trim() ?? ""
                                };

                                // Parse units_in (Energy Imported)
                                //if (decimal.TryParse(reader["units_in"]?.ToString(), out decimal unitsIn))
                                //{
                                    //energyData.EnergyImported = unitsIn;
                                //}
                                energyData.EnergyImported = reader["units_in"]?.ToString()?.Trim() ?? "";

                                // Parse units_out (Energy Exported)
                                //if (decimal.TryParse(reader["units_out"]?.ToString(), out decimal unitsOut))
                                //{
                                 //   energyData.EnergyExported = unitsOut;
                                //}
                                energyData.EnergyExported = reader["units_out"]?.ToString()?.Trim() ?? "";

                                // Parse gen_cap (Generation Capacity)
                                //if (decimal.TryParse(reader["gen_cap"]?.ToString(), out decimal genCap))
                                //{
                                //    energyData.GenerationCapacity = genCap;
                                //}

                                if (decimal.TryParse(reader["rate"]?.ToString(), out decimal rate))
                                {
                                    energyData.UnitCost = rate;  //  Unit Cost from rate
                                }
                                // Parse unitsale (Unit Sale/Cost)
                                if (decimal.TryParse(reader["unitsale"]?.ToString(), out decimal unitSale))
                                {
                                    energyData.UnitSale = unitSale;
                                }
                                // ASSIGN METER NUMBERS FROM customerInfo
                                energyData.MeterNumber1 = customerInfo.MeterNumber1;
                                energyData.MeterNumber2 = customerInfo.MeterNumber2;
                                energyData.MeterNumber3 = customerInfo.MeterNumber3;

                                energyHistory.Add(energyData);
                            }
                        }
                    }

                    System.Diagnostics.Trace.WriteLine($"Retrieved {energyHistory.Count} energy history records");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetEnergyHistory: {ex.Message}");
                throw;
            }

            return energyHistory;
        }

        // Helper method to calculate start cycle for last N months
        private string CalculateStartCycle(string latestCycle, int monthsBack)
        {
            try
            {
                // Bill cycle is just a sequential number (e.g., 446)
                // Subtract monthsBack from it
                int cycle = int.Parse(latestCycle);
                int startCycle = cycle - monthsBack;

                System.Diagnostics.Trace.WriteLine($"Calculated start cycle: {startCycle} (from {latestCycle}, {monthsBack} cycles back)");

                return startCycle.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in CalculateStartCycle: {ex.Message}");
                return latestCycle;
            }
        }

        // Check if account exists in ordinary database
        public bool AccountExists(string accountNumber)
        {
            try
            {
                using (var conn = _dbConnection.GetConnection(false))
                {
                    conn.Open();

                    string query = "SELECT COUNT(*) FROM netmtcons WHERE acct_number = ?";

                    using (var cmd = new OleDbCommand(query, conn))
                    {
                        cmd.Parameters.Add("@acct_number", OleDbType.VarChar).Value = accountNumber;

                        var result = cmd.ExecuteScalar();
                        int count = Convert.ToInt32(result);

                        System.Diagnostics.Trace.WriteLine($"Account {accountNumber} exists: {count > 0}");
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in AccountExists: {ex.Message}");
                return false;
            }
        }
        // Helper class for area information
        private class AreaInformation
        {
            public string AreaName { get; set; }
            public string ProvinceCode { get; set; }
            public string ProvinceName { get; set; }
            public string Region { get; set; }
        }
    }
}