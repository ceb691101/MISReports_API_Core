// Bank Paid PIV Details (paid through bank)
using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class BankPaidPIVDetailsRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<BankPaidPIVDetailsModel> GetBankPaidPIVDetails(
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<BankPaidPIVDetailsModel>();

            string sql = @"
SELECT DISTINCT
    c.PAID_DATE,
    c.BANK_CHECK_NO,
    c.PAID_AGENT,
    c.PAID_BRANCH,
    c.PAID_AMOUNT,
    c.dept_id
FROM
    piv_detail c
WHERE
    c.paid_dept_id = '000.00'
    AND c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
    AND c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
ORDER BY
    c.PAID_DATE
";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new BankPaidPIVDetailsModel
                            {
                                PaidDate = reader["PAID_DATE"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("PAID_DATE")),
                                BankCheckNo = reader["BANK_CHECK_NO"]?.ToString(),
                                PaidAgent = reader["PAID_AGENT"]?.ToString(),
                                PaidBranch = reader["PAID_BRANCH"]?.ToString(),
                                PaidAmount = reader["PAID_AMOUNT"] == DBNull.Value ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("PAID_AMOUNT")),
                                DeptId = reader["dept_id"]?.ToString()
                            };

                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching Bank Paid PIV details: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}