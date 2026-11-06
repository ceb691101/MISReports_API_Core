using MISReports_Api.Compat;
using MISReports_Api.DBAccess;
using MISReports_Api.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace MISReports_Api.DAL
{
    public class DebtorRepository
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string connectionString = ConfigurationManager.ConnectionStrings["InformixConnection"].ConnectionString;

        public List<DebtorDetailModel> GetDebtorDetails(DebtorRequest request)
        {
            var debtorsList = new List<DebtorDetailModel>();
            string sql = BuildSqlQuery(request);

            using (var conn = Informix.CreateConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    // Values are bound to the ? placeholders in order: cust_type, bill_cycle, area_code
                    using (var cmd = DbCommandHelper.CreateOleDbCommand(sql, conn,
                        request.CustType ?? string.Empty, request.BillCycle ?? string.Empty, request.AreaCode ?? string.Empty))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var debtor = MapDebtorFromReader(reader, request.AgeRange);
                            debtorsList.Add(debtor);
                        }
                    }
                }
                catch (DbException ex)
                {
                    logger.Error(ex, "GetDebtorDetails failed (exception rethrown)");
                    Console.WriteLine($"Error retrieving debtor details: {ex.Message}", ex);
                    throw;
                }
            }

            return debtorsList;
        }

        private string BuildSqlQuery(DebtorRequest request)
        {
            switch (request.AgeRange)
            {
                case AgeRange.Months0_6:
                    return $@"SELECT 
                        r.area_name, a.acct_number, a.tariff_code, a.out_bal, 
                        a.cust_fname, a.cust_lname, a.address_1, a.address_2, a.address_3,
                        m00, m01, m02, m03, m04, m05, m06
                        FROM ageacct a, areas r 
                        WHERE a.cust_type=? 
                        AND a.bill_cycle=? 
                        AND a.area_code=r.area_code 
                        AND a.area_code=?
                        AND (m00 <> 0 OR m01<>0 OR m02<>0 OR m03<>0 OR m04<>0 OR m05<>0 OR m06<>0) 
                        ORDER BY a.out_bal DESC";

                case AgeRange.Months7_12:
                    return $@"SELECT 
                        r.area_name, a.acct_number, a.tariff_code, a.out_bal, 
                        a.cust_fname, a.cust_lname, a.address_1, a.address_2, a.address_3,
                        m07, m08, m09, m10, m11, m12, 0
                        FROM ageacct a, areas r 
                        WHERE a.cust_type=? 
                        AND a.bill_cycle=? 
                        AND a.area_code=r.area_code 
                        AND a.area_code=?
                        AND (m07 <> 0 OR m08<>0 OR m09<>0 OR m10<>0 OR m11<>0 OR m12<>0) 
                        ORDER BY a.out_bal DESC";

                case AgeRange.Years1_2:
                    return $@"SELECT 
                        r.area_name, a.acct_number, a.tariff_code, a.out_bal, 
                        a.cust_fname, a.cust_lname, a.address_1, a.address_2, a.address_3,
                        m13, m14, m15, m16, m17, m18, m19, m20, m21, m22, m23, m24
                        FROM ageacct a, areas r 
                        WHERE a.cust_type=? 
                        AND a.bill_cycle=? 
                        AND a.area_code=r.area_code 
                        AND a.area_code=?
                        AND (m13 <> 0 OR m14<>0 OR m15<>0 OR m16<>0 OR m17<>0 OR m18<>0 OR 
                             m19<>0 OR m20<>0 OR m21<>0 OR m22<>0 OR m23<>0 OR m24<>0) 
                        ORDER BY a.out_bal DESC";

                case AgeRange.Years2_3:
                    return $@"SELECT 
                        r.area_name, a.acct_number, a.tariff_code, a.out_bal, 
                        a.cust_fname, a.cust_lname, a.address_1, a.address_2, a.address_3,
                        m25, m26, m27, m28, m29, m30, m31, m32, m33, m34, m35, m36
                        FROM ageacct a, areas r 
                        WHERE a.cust_type=? 
                        AND a.bill_cycle=? 
                        AND a.area_code=r.area_code 
                        AND a.area_code=?
                        AND (m25 <> 0 OR m26<>0 OR m27<>0 OR m28<>0 OR m29<>0 OR m30<>0 OR 
                             m31<>0 OR m32<>0 OR m33<>0 OR m34<>0 OR m35<>0 OR m36<>0) 
                        ORDER BY a.out_bal DESC";

                case AgeRange.Years3_4:
                    return $@"SELECT 
                        r.area_name, a.acct_number, a.tariff_code, a.out_bal, 
                        a.cust_fname, a.cust_lname, a.address_1, a.address_2, a.address_3,
                        m37, m38, m39, m40, m41, m42, m43, m44, m45, m46, m47, m48
                        FROM ageacct a, areas r 
                        WHERE a.cust_type=? 
                        AND a.bill_cycle=? 
                        AND a.area_code=r.area_code 
                        AND a.area_code=?
                        AND (m37 <> 0 OR m38<>0 OR m39<>0 OR m40<>0 OR m41<>0 OR m42<>0 OR 
                             m43<>0 OR m44<>0 OR m45<>0 OR m46<>0 OR m47<>0 OR m48<>0) 
                        ORDER BY a.out_bal DESC";

                case AgeRange.Years4_5:
                    return $@"SELECT 
                        r.area_name, a.acct_number, a.tariff_code, a.out_bal, 
                        a.cust_fname, a.cust_lname, a.address_1, a.address_2, a.address_3,
                        m49, m50, m51, m52, m53, m54, m55, m56, m57, m58, m59, m60
                        FROM ageacct a, areas r 
                        WHERE a.cust_type=? 
                        AND a.bill_cycle=? 
                        AND a.area_code=r.area_code 
                        AND a.area_code=?
                        AND (m49 <> 0 OR m50<>0 OR m51<>0 OR m52<>0 OR m53<>0 OR m54<>0 OR 
                             m55<>0 OR m56<>0 OR m57<>0 OR m58<>0 OR m59<>0 OR m60<>0) 
                        ORDER BY a.out_bal DESC";

                case AgeRange.Years5Plus:
                    return $@"SELECT 
                        r.area_name, a.acct_number, a.tariff_code, a.out_bal, 
                        a.cust_fname, a.cust_lname, a.address_1, a.address_2, a.address_3,
                        m61
                        FROM ageacct a, areas r 
                        WHERE a.cust_type=? 
                        AND a.bill_cycle=? 
                        AND a.area_code=r.area_code 
                        AND a.area_code=?
                        AND m61 <> 0 
                        ORDER BY a.out_bal DESC";

                case AgeRange.All:
                default:
                    return $@"SELECT 
                        r.area_name, a.acct_number, a.tariff_code, a.out_bal, 
                        a.cust_fname, a.cust_lname, a.address_1, a.address_2, a.address_3,
                        m00, m01, m02, m03, m04, m05, m06,
                        (m07+m08+m09), (m10+m11+m12),
                        (m13+m14+m15+m16+m17+m18+m19+m20+m21+m22+m23+m24),
                        (m25+m26+m27+m28+m29+m30+m31+m32+m33+m34+m35+m36),
                        (m37+m38+m39+m40+m41+m42+m43+m44+m45+m46+m47+m48),
                        (m49+m50+m51+m52+m53+m54+m55+m56+m57+m58+m59+m60), 
                        m61
                        FROM ageacct a, areas r 
                        WHERE a.cust_type=? 
                        AND a.bill_cycle=? 
                        AND a.area_code=r.area_code 
                        AND a.area_code=?
                        AND (m00 <> 0 OR m01<>0 OR m02<>0 OR m03<>0 OR m04<>0 OR m05<>0 OR m06<>0 OR 
                             (m07+m08+m09)<>0 OR (m10+m11+m12)<>0 OR 
                             (m13+m14+m15+m16+m17+m18+m19+m20+m21+m22+m23+m24)<>0 OR 
                             (m25+m26+m27+m28+m29+m30+m31+m32+m33+m34+m35+m36)<>0 OR 
                             (m37+m38+m39+m40+m41+m42+m43+m44+m45+m46+m47+m48)<>0 OR 
                             (m49+m50+m51+m52+m53+m54+m55+m56+m57+m58+m59+m60)<>0 OR m61<>0) 
                        ORDER BY a.out_bal DESC";
            }
        }

        private DebtorDetailModel MapDebtorFromReader(DbDataReader reader, AgeRange ageRange)
        {
            var debtor = new DebtorDetailModel
            {
                AreaName = reader[0]?.ToString().Trim(),
                AccountNumber = reader[1]?.ToString().Trim(),
                TariffCode = reader[2]?.ToString().Trim(),
                OutstandingBalance = reader[3] != DBNull.Value ? Convert.ToDecimal(reader[3]) : 0,
                FirstName = reader[4]?.ToString().Trim(),
                LastName = reader[5]?.ToString().Trim(),
                Address1 = reader[6]?.ToString().Trim(),
                Address2 = reader[7]?.ToString().Trim(),
                Address3 = reader[8]?.ToString().Trim()
            };

            switch (ageRange)
            {
                case AgeRange.Months0_6:
                    debtor.Month0 = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                    debtor.Month1 = reader[10] != DBNull.Value ? Convert.ToDecimal(reader[10]) : 0;
                    debtor.Month2 = reader[11] != DBNull.Value ? Convert.ToDecimal(reader[11]) : 0;
                    debtor.Month3 = reader[12] != DBNull.Value ? Convert.ToDecimal(reader[12]) : 0;
                    debtor.Month4 = reader[13] != DBNull.Value ? Convert.ToDecimal(reader[13]) : 0;
                    debtor.Month5 = reader[14] != DBNull.Value ? Convert.ToDecimal(reader[14]) : 0;
                    debtor.Month6 = reader[15] != DBNull.Value ? Convert.ToDecimal(reader[15]) : 0;
                    break;

                case AgeRange.Months7_12:
                    debtor.Months7_9 = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                    debtor.Months10_12 = reader[10] != DBNull.Value ? Convert.ToDecimal(reader[10]) : 0;
                    break;

                case AgeRange.Years1_2:
                    debtor.Months13_24 = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                    break;

                case AgeRange.Years2_3:
                    debtor.Months25_36 = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                    break;

                case AgeRange.Years3_4:
                    debtor.Months37_48 = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                    break;

                case AgeRange.Years4_5:
                    debtor.Months49_60 = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                    break;

                case AgeRange.Years5Plus:
                    debtor.Months61Plus = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                    break;

                case AgeRange.All:
                default:
                    debtor.Month0 = reader[9] != DBNull.Value ? Convert.ToDecimal(reader[9]) : 0;
                    debtor.Month1 = reader[10] != DBNull.Value ? Convert.ToDecimal(reader[10]) : 0;
                    debtor.Month2 = reader[11] != DBNull.Value ? Convert.ToDecimal(reader[11]) : 0;
                    debtor.Month3 = reader[12] != DBNull.Value ? Convert.ToDecimal(reader[12]) : 0;
                    debtor.Month4 = reader[13] != DBNull.Value ? Convert.ToDecimal(reader[13]) : 0;
                    debtor.Month5 = reader[14] != DBNull.Value ? Convert.ToDecimal(reader[14]) : 0;
                    debtor.Month6 = reader[15] != DBNull.Value ? Convert.ToDecimal(reader[15]) : 0;
                    debtor.Months7_9 = reader[16] != DBNull.Value ? Convert.ToDecimal(reader[16]) : 0;
                    debtor.Months10_12 = reader[17] != DBNull.Value ? Convert.ToDecimal(reader[17]) : 0;
                    debtor.Months13_24 = reader[18] != DBNull.Value ? Convert.ToDecimal(reader[18]) : 0;
                    debtor.Months25_36 = reader[19] != DBNull.Value ? Convert.ToDecimal(reader[19]) : 0;
                    debtor.Months37_48 = reader[20] != DBNull.Value ? Convert.ToDecimal(reader[20]) : 0;
                    debtor.Months49_60 = reader[21] != DBNull.Value ? Convert.ToDecimal(reader[21]) : 0;
                    debtor.Months61Plus = reader[22] != DBNull.Value ? Convert.ToDecimal(reader[22]) : 0;
                    break;
            }

            return debtor;
        }
    }
}