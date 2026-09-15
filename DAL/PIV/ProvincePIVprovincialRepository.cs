// File: Repositories/ProvincePIVprovincialRepository.cs
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.Repositories
{
    public class ProvincePIVprovincialRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvincePIVprovincialPOSModel> GetReport(
            string compId, DateTime fromDate, DateTime toDate)
        {
            var result = new List<ProvincePIVprovincialPOSModel>();

            string sql = @"
                SELECT DISTINCT
                    c.paid_dept_id AS Cost_Center,
                    c.dept_id AS Dept_Id,
                    c.piv_no AS Piv_No,
                    c.piv_receipt_no AS Piv_Receipt_No,
                    c.piv_date AS Piv_Date,
                    c.paid_date AS Paid_Date,
                    c.payment_mode AS Payment_Mode,
                    c.cheque_no AS Cheque_No,
                    c.grand_total AS Grand_Total,
                    c.bank_check_no AS Bank_Check_No,
                    a.account_code AS Account_Code,
                    a.amount AS Amount,
                    (SELECT dept_nm FROM gldeptm WHERE dept_id = c.paid_dept_id) AS CCT_NAME,
                    (SELECT comp_nm FROM glcompm WHERE comp_id = :compId) AS COMPANY_NAME
                FROM piv_detail c
                INNER JOIN piv_amount a
                    ON c.piv_no = a.piv_no
                   AND c.dept_id = a.dept_id
                WHERE TRIM(c.status) IN ('Q', 'P', 'F', 'FR', 'FA')
                  AND c.paid_dept_id IN (
                    SELECT x.dept_id
                    FROM gldeptm x
                    WHERE x.comp_id IN (
                        SELECT comp_id FROM glcompm
                        WHERE comp_id = :compId OR parent_id = :compId
                    )
                  )
                  AND c.dept_id IN (
                    SELECT x.dept_id
                    FROM gldeptm x
                    WHERE x.comp_id IN (
                        SELECT comp_id FROM glcompm
                        WHERE comp_id = :compId OR parent_id = :compId
                        and  c.paid_dept_id !='000.00'

                    )
                  )
                  AND c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
                  AND c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
                ORDER BY
                    c.paid_dept_id, c.dept_id, c.piv_no,
                    c.piv_receipt_no, c.piv_date, c.paid_date,
                    c.payment_mode, c.cheque_no, c.grand_total,
                    a.account_code, a.amount";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;

                cmd.Parameters.Add(new OracleParameter("compId", OracleDbType.Varchar2) { Value = compId });
                cmd.Parameters.Add(new OracleParameter("fromDate", OracleDbType.Varchar2) { Value = fromDate.ToString("yyyy/MM/dd") });
                cmd.Parameters.Add(new OracleParameter("toDate", OracleDbType.Varchar2) { Value = toDate.ToString("yyyy/MM/dd") });

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new ProvincePIVprovincialPOSModel
                        {
                            Cost_Center = reader["Cost_Center"]?.ToString() ?? "",
                            Dept_Id = reader["Dept_Id"]?.ToString() ?? "",
                            Piv_No = reader["Piv_No"]?.ToString() ?? "",
                            Piv_Receipt_No = reader["Piv_Receipt_No"]?.ToString() ?? "",
                            Piv_Date = (DateTime)(reader["Piv_Date"] != DBNull.Value
                                ? Convert.ToDateTime(reader["Piv_Date"])
                                : (DateTime?)null),
                            Paid_Date = (DateTime)(reader["Paid_Date"] != DBNull.Value
                                ? Convert.ToDateTime(reader["Paid_Date"])
                                : (DateTime?)null),
                            Payment_Mode = reader["Payment_Mode"]?.ToString() ?? "",
                            Cheque_No = reader["Cheque_No"]?.ToString() ?? "",
                            Grand_Total = Convert.ToDecimal(reader["Grand_Total"] ?? 0),
                            Bank_Check_No = reader["Bank_Check_No"]?.ToString() ?? "",
                            CCT_NAME = reader["CCT_NAME"]?.ToString() ?? "",
                            Account_Code = reader["Account_Code"]?.ToString() ?? "",
                            Amount = Convert.ToDecimal(reader["Amount"] ?? 0)
                        };

                        result.Add(item);
                    }
                }
            }

            return result;
        }
    }
}