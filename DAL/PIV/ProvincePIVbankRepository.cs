//01.Branch/Province wise PIV Collections Paid to Bank

using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.Repositories
{
    public class ProvincePIVbankRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvincePIVbankModel> GetProvincePIVbankReport(
            string compId,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<ProvincePIVbankModel>();

            string sql = @"
        SELECT
            c.dept_id AS CostCenter,
            c.piv_no AS PivNo,
            c.piv_receipt_no AS PivReceiptNo,
            c.piv_date AS PivDate,
            c.paid_date AS PaidDate,
            c.cheque_no AS ChequeNo,
            c.bank_check_no AS BankCheckNo,
            c.paid_amount AS GrandTotal,
            CASE
                WHEN c.payment_mode = 'R' THEN 'Credit Card'
                WHEN c.payment_mode = 'Q' THEN 'Cheque'
                WHEN c.payment_mode = 'D' THEN 'Bank Draft'
                WHEN c.payment_mode = 'C' THEN 'Cash'
                ELSE c.payment_mode
            END AS PaymentMode,
            (SELECT dept_nm FROM gldeptm WHERE dept_id = c.dept_id) AS CCT_NAME,
            (SELECT comp_nm FROM glcompm WHERE comp_id = :compId) AS COMPANY_NAME,
            a.account_code AS AccountCode,
            a.amount AS Amount
        FROM piv_detail c
        INNER JOIN piv_amount a ON c.piv_no = a.piv_no AND c.dept_id = a.dept_id
        WHERE TRIM(c.status) IN ('Q', 'P', 'F', 'FR', 'FA')
          AND c.paid_dept_id = '000.00'
          AND c.paid_date >= :fromDate
          AND c.paid_date <= :toDate
          AND c.dept_id IN (
              SELECT x.dept_id
              FROM gldeptm x
              WHERE x.comp_id IN (
                  SELECT comp_id FROM glcompm
                  WHERE parent_id = :compId OR comp_id = :compId
              )
          )
        ORDER BY c.dept_id, c.piv_no, a.account_code";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add(new OracleParameter("compId", OracleDbType.Varchar2) { Value = compId });
                cmd.Parameters.Add(new OracleParameter("fromDate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("toDate", OracleDbType.Date) { Value = toDate.Date });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new ProvincePIVbankModel
                        {
                            CostCenter = reader["CostCenter"]?.ToString() ?? "",
                            PivNo = reader["PivNo"]?.ToString() ?? "",
                            PivReceiptNo = reader["PivReceiptNo"]?.ToString() ?? "",
                            PivDate = reader["PivDate"] != DBNull.Value ? Convert.ToDateTime(reader["PivDate"]) : (DateTime?)null,
                            PaidDate = reader["PaidDate"] != DBNull.Value ? Convert.ToDateTime(reader["PaidDate"]) : (DateTime?)null,
                            ChequeNo = reader["ChequeNo"]?.ToString() ?? "",
                            BankCheckNo = reader["BankCheckNo"]?.ToString() ?? "",
                            GrandTotal = reader["GrandTotal"] != DBNull.Value ? Convert.ToDecimal(reader["GrandTotal"]) : 0m,
                            PaymentMode = reader["PaymentMode"]?.ToString() ?? "",
                            CCT_NAME = reader["CCT_NAME"]?.ToString() ?? "",
                            COMPANY_NAME = reader["COMPANY_NAME"]?.ToString() ?? "",
                            AccountCode = reader["AccountCode"]?.ToString() ?? "",
                            Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0m
                        };
                        result.Add(item);
                    }
                }
            }

            return result;
        }
    }
}