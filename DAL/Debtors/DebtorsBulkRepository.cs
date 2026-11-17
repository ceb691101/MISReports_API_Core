using MISReports_Api.Compat;
using MISReports_Api.Models;
using MISReports_Api.DBAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MISReports_Api.DAL
{
    public class DebtorsBulkRepository
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixBulkConnection"].ConnectionString;

        public List<DebtorsBulkModel> GetDebtorsBulkData(string opt, string cycle, string areaCode)
        {
            var debtorsList = new List<DebtorsBulkModel>();

            using (var conn = Informix.CreateConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    var parameters = new List<object>();
                    string sql = BuildSqlQuery(opt, cycle, areaCode, parameters);

                    using (var cmd = DbCommandHelper.CreateOleDbCommand(sql, conn, parameters.ToArray()))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var debtor = new DebtorsBulkModel
                            {
                                Type = "Bulk",
                                CustType = GetCustomerTypeDescription(reader[0]?.ToString().Trim()),
                                TotDebtors = reader[1] != DBNull.Value ? Convert.ToDecimal(reader[1]) : 0,
                                Month01 = reader[2] != DBNull.Value ? Convert.ToDecimal(reader[2]) : 0,
                                Month02 = reader[3] != DBNull.Value ? Convert.ToDecimal(reader[3]) : 0,
                                Month03 = reader[4] != DBNull.Value ? Convert.ToDecimal(reader[4]) : 0,
                                Month04 = reader[5] != DBNull.Value ? Convert.ToDecimal(reader[5]) : 0
                            };

                            debtorsList.Add(debtor);
                        }
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "GetDebtorsBulkData failed (exception rethrown). opt={opt}, cycle={cycle}, areaCode={areaCode}", opt, cycle, areaCode);
                    Console.WriteLine($"Error retrieving bulk debtors data: {ex.Message}", ex);
                    throw;
                }
            }
#if true

#endif
            return debtorsList;
        }

        // Builds the SQL with ? placeholders and adds the matching values, in order, to 'parameters'.
        private string BuildSqlQuery(string opt, string cycle, string areaCode, List<object> parameters)
        {
            string baseSelect = @"SELECT c.cust_cd, 
                SUM(a.bal_mon_end), 
                SUM(a.age_0), 
                SUM(a.age_1), 
                SUM(a.age_2), 
                SUM(a.age_3 + a.age_4 + a.age_5 + a.age_6 + a.age_7 + a.age_8 + a.age_9 + 
                    a.age_10 + a.age_11 + a.age_12 + a.Over_12 + a.Over_24 + a.Over_36) ";

            switch (opt?.ToUpper())
            {
                case "A":
                    parameters.Add(cycle ?? string.Empty);
                    parameters.Add(DbCommandHelper.RequireInteger(areaCode, "areaCode"));
                    return $@"{baseSelect}
                        FROM age_info_new a, customer c 
                        WHERE a.acc_nbr = c.acc_nbr AND c.cust_cd <> 'T' 
                        AND a.bill_cycle = ? AND a.area_cd = ? 
                        GROUP BY c.cust_cd 
                        ORDER BY c.cust_cd";

                case "P":
                    if (!(Convert.ToInt32(areaCode[0]) >= 48 && Convert.ToInt32(areaCode[0]) <= 57))
                    {
                        parameters.Add(areaCode ?? string.Empty);
                        parameters.Add(cycle ?? string.Empty);
                        return $@"{baseSelect}
                            FROM age_info_new a, customer c, areas a2 
                            WHERE a.acc_nbr = c.acc_nbr AND c.cust_cd <> 'T' AND a.area_cd = a2.area_code 
                            AND a2.prov_code = ? 
                            AND a.bill_cycle = ? 
                            GROUP BY c.cust_cd 
                            ORDER BY c.cust_cd";
                    }
                    else
                    {
                        parameters.Add("0" + areaCode);
                        parameters.Add(cycle ?? string.Empty);
                        return $@"{baseSelect}
                            FROM age_info_new a, customer c, areas a2 
                            WHERE a.acc_nbr = c.acc_nbr AND c.cust_cd <> 'T' AND a.area_cd = a2.area_code 
                            AND a2.prov_code = ? 
                            AND a.bill_cycle = ? 
                            GROUP BY c.cust_cd 
                            ORDER BY c.cust_cd";
                    }

                case "D":
                    parameters.Add(areaCode ?? string.Empty);
                    parameters.Add(cycle ?? string.Empty);
                    return $@"{baseSelect}
                        FROM age_info_new a, customer c, areas a2 
                        WHERE a.acc_nbr = c.acc_nbr AND c.cust_cd <> 'T' AND a.area_cd = a2.area_code 
                        AND a2.region = ? 
                        AND a.bill_cycle = ? 
                        GROUP BY c.cust_cd 
                        ORDER BY c.cust_cd";

                case "E":
                    parameters.Add(cycle ?? string.Empty);
                    return $@"{baseSelect}
                        FROM age_info_new a, customer c 
                        WHERE a.acc_nbr = c.acc_nbr AND c.cust_cd <> 'T' 
                        AND a.bill_cycle = ? 
                        GROUP BY c.cust_cd 
                        ORDER BY c.cust_cd";

                default:
                    throw new ArgumentException($"Invalid option: {opt}");
            }
        }

        private string GetCustomerTypeDescription(string custType)
        {
            if (string.IsNullOrEmpty(custType))
                return "Unknown";

            switch (custType.ToUpper())
            {
                case "G":
                    return "Govt.";
                case "P":
                    return "Private";
                case "S":
                    return "Semi Govt.";
                default:
                    return custType;
            }
        }
    }
}