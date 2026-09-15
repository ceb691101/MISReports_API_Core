// File: Repositories/ProvincePIVAllRepository.cs
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.Repositories
{
    public class ProvincePIVAllRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvincePIVAllModel> GetProvincePIVAllReport(
            string compId,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<ProvincePIVAllModel>();

            string sql = @"
                SELECT DISTINCT
                    c.paid_dept_id AS paid_dept_id,
                    c.dept_id AS dept_id,
                    c.piv_no,
                    c.piv_receipt_no,
                    c.piv_date,
                    c.paid_date,
                    c.payment_mode,
                    c.cheque_no,
                    c.grand_total,
                    a.account_code AS Account_Code,
                    a.amount,
                    c.bank_check_no,
                    (SELECT dept_nm
                       FROM gldeptm
                      WHERE dept_id = c.paid_dept_id) AS CCT_NAME,
                    (SELECT comp_nm
                       FROM glcompm
                      WHERE TRIM(comp_id) = :compId) AS CCT_NAME1
                FROM piv_amount a
                INNER JOIN piv_detail c
                    ON c.PIV_NO = a.PIV_NO
                   AND a.dept_id = c.dept_id
                WHERE TRIM(c.status) IN ('Q', 'P', 'F', 'FR', 'FA')
                  AND c.paid_dept_id IN (
                        SELECT x.dept_id
                        FROM gldeptm x
                        WHERE x.comp_id IN (
                            SELECT comp_id
                            FROM glcompm
                            WHERE TRIM(parent_id) = :compId OR TRIM(comp_id) = :compId
                        )
                  )
                  AND c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
                  AND c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
                GROUP BY
                    c.paid_dept_id,
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
                    c.paid_dept_id,
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
                        var item = new ProvincePIVAllModel
                        {
                            Paid_Dept_Id = reader["paid_dept_id"]?.ToString() ?? "",
                            Dept_Id = reader["dept_id"]?.ToString() ?? "",
                            Piv_No = reader["piv_no"]?.ToString() ?? "",
                            Piv_Receipt_No = reader["piv_receipt_no"]?.ToString() ?? "",
                            Piv_Date = reader["piv_date"] != DBNull.Value
                                ? Convert.ToDateTime(reader["piv_date"])
                                : (DateTime?)null,
                            Paid_Date = reader["paid_date"] != DBNull.Value
                                ? Convert.ToDateTime(reader["paid_date"])
                                : (DateTime?)null,
                            Payment_Mode = reader["payment_mode"]?.ToString() ?? "",
                            Cheque_No = reader["cheque_no"]?.ToString() ?? "",
                            Grand_Total = reader["grand_total"] != DBNull.Value
                                ? Convert.ToDecimal(reader["grand_total"])
                                : 0m,
                            Account_Code = reader["Account_Code"]?.ToString() ?? "",
                            Amount = reader["amount"] != DBNull.Value
                                ? Convert.ToDecimal(reader["amount"])
                                : 0m,
                            Bank_Check_No = reader["bank_check_no"]?.ToString() ?? "",
                            CCT_NAME = reader["CCT_NAME"]?.ToString() ?? "",
                            CCT_NAME1 = reader["CCT_NAME1"]?.ToString() ?? ""
                        };

                        result.Add(item);
                    }
                }
            }

            return result;
        }
    }
}