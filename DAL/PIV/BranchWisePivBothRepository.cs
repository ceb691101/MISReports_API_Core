// 06. Branch wise PIV Tabulation (Both Bank and POS) Report

using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.Repositories
{
    public class BranchWisePivBothRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<BranchWisePivBothModel> GetBranchWisePivBothReport(
            string compId,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<BranchWisePivBothModel>();

            string sql = @"
                SELECT DISTINCT
                    c.dept_id AS Issued_cost_center,
                    c.piv_no,
                    c.piv_receipt_no,
                    c.piv_date,
                    c.paid_date,
                    c.payment_mode,
                    c.cheque_no,
                    c.grand_total,
                    a.account_code,
                    a.amount,
                    c.bank_check_no,
                    (SELECT dept_nm FROM gldeptm WHERE Trim(dept_id) = c.dept_id) AS issued_cc_name,
                    (SELECT comp_nm FROM glcompm WHERE Trim(comp_id) = :compId) AS company_name
                FROM piv_amount a, piv_detail c
                WHERE c.PIV_NO = a.PIV_NO
                  AND a.dept_id = c.dept_id
                  AND TRIM(c.status) IN ('Q', 'P', 'F', 'FR', 'FA')
                  AND c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
                  AND c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
                  AND c.dept_id IN (
                        SELECT X.dept_id
                        FROM gldeptm X
                        WHERE X.comp_id IN (
                              SELECT comp_id
                              FROM glcompm
                              WHERE Trim(parent_id) = :compId OR Trim(comp_id) = :compId
                        )
                  )
                GROUP BY
                    c.dept_id,
                    c.piv_no,
                    c.piv_receipt_no,
                    c.piv_date,
                    c.paid_date,
                    c.payment_mode,
                    c.cheque_no,
                    c.grand_total,
                    a.account_code,
                    a.amount,
                    c.bank_check_no
                ORDER BY
                    c.dept_id,
                    c.piv_no,
                    c.piv_receipt_no,
                    c.piv_date,
                    c.paid_date,
                    c.payment_mode,
                    c.cheque_no,
                    c.grand_total,
                    a.account_code,
                    a.amount";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId?.Trim() ?? "";
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new BranchWisePivBothModel
                        {
                            Issued_cost_center = reader["Issued_cost_center"].ToString(),
                            Piv_no = reader["piv_no"].ToString(),
                            Piv_date = reader["piv_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv_date"]),
                            Paid_date = reader["paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["paid_date"]),
                            Payment_mode = reader["payment_mode"].ToString(),
                            Grand_total = reader["grand_total"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["grand_total"]),
                            Account_code = reader["account_code"].ToString(),
                            Amount = reader["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["amount"]),
                            Bank_check_no = reader["bank_check_no"].ToString(),
                            Issued_cc_name = reader["issued_cc_name"].ToString(),
                            Company_name = reader["company_name"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}