using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class BankPivTabulationRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<BankPivTabulationModel> GetBankPivTabulation(
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<BankPivTabulationModel>();

            string sql = @"
SELECT DISTINCT
    c.dept_id AS C6,
    c.piv_no,
    c.piv_receipt_no,
    c.piv_date,
    c.paid_date,
    c.cheque_no,
    c.grand_total,
    a.account_code AS c8,
    a.amount,
    c.bank_check_no,
    (CASE
        WHEN c.payment_mode = 'R' THEN 'Credit Card'
        WHEN c.payment_mode = 'Q' THEN 'Cheque'
        WHEN c.payment_mode = 'D' THEN 'Bank Draft'
        WHEN c.payment_mode = 'C' THEN 'Cash'
        ELSE c.payment_mode
    END) AS payment_mode,
    (SELECT dept_nm FROM gldeptm WHERE dept_id = c.dept_id) AS CCT_NAME
FROM piv_amount a, piv_detail c
WHERE c.PIV_NO = a.PIV_NO
  AND a.dept_id = c.dept_id
  AND TRIM(c.status) IN ('Q', 'P', 'F', 'FR', 'FA')
  AND c.paid_dept_id = '000.00'
  AND c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  AND c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
GROUP BY
    c.dept_id,
    c.piv_no,
    c.piv_receipt_no,
    c.piv_date,
    c.paid_date,
    (CASE
        WHEN c.payment_mode = 'R' THEN 'Credit Card'
        WHEN c.payment_mode = 'Q' THEN 'Cheque'
        WHEN c.payment_mode = 'D' THEN 'Bank Draft'
        WHEN c.payment_mode = 'C' THEN 'Cash'
        ELSE c.payment_mode
    END),
    c.cheque_no,
    c.grand_total,
    a.account_code,
    a.amount,
    c.bank_check_no,
    c.payment_mode
ORDER BY
    c.dept_id,
    c.piv_no,
    c.piv_receipt_no,
    c.piv_date,
    c.paid_date,
    c.cheque_no,
    c.grand_total,
    a.account_code,
    a.amount
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
                            var item = new BankPivTabulationModel
                            {
                                C6 = reader["C6"]?.ToString(),
                                PivNo = reader["piv_no"]?.ToString(),
                                PivReceiptNo = reader["piv_receipt_no"]?.ToString(),
                                PivDate = reader["piv_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("piv_date")),
                                PaidDate = reader["paid_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("paid_date")),
                                ChequeNo = reader["cheque_no"]?.ToString(),
                                GrandTotal = reader["grand_total"] == DBNull.Value ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("grand_total")),
                                C8 = reader["c8"]?.ToString(),
                                Amount = reader["amount"] == DBNull.Value ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("amount")),
                                BankCheckNo = reader["bank_check_no"]?.ToString(),
                                PaymentMode = reader["payment_mode"]?.ToString(),
                                CctName = reader["CCT_NAME"]?.ToString()
                            };
                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching Bank PIV Tabulation data: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}