//23.Region wise PIV Collections by Provincial POS relevant to Other Cost Centers

using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.Repositories
{
    public class RegionPivFromOtherCCRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<RegionPivFromOtherCCModel> GetRegionPivFromOtherCC(
            string compId,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<RegionPivFromOtherCCModel>();

            string sql = @"
SELECT DISTINCT
    c.paid_dept_id AS C6,
    c.dept_id AS dept_id,
    c.piv_no,
    c.piv_receipt_no,
    c.piv_date,
    c.paid_date,
    c.payment_mode,
    c.cheque_no,
    c.grand_total,
    a.account_code AS c8,
    a.amount,
    c.bank_check_no,
    (SELECT dept_nm FROM gldeptm WHERE dept_id = c.paid_dept_id) AS CCT_NAME,
    (SELECT comp_nm FROM glcompm WHERE comp_id = :compId) AS CCT_NAME1
FROM piv_amount a
JOIN piv_detail c ON c.PIV_NO = a.PIV_NO AND a.dept_id = c.dept_id
WHERE TRIM(c.status) IN ('Q', 'P', 'F', 'FR', 'FA')
  AND c.paid_dept_id IN (
        SELECT X.dept_id
        FROM gldeptm X
        WHERE X.comp_id IN (
              SELECT comp_id
              FROM glcompm
              WHERE parent_id = :compId
                 OR comp_id = :compId
                 OR grp_comp = :compId
        )
  )
  AND c.paid_dept_id != '000.00'
  AND c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  AND c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
  AND c.dept_id NOT IN (
        SELECT X.dept_id
        FROM gldeptm X
        WHERE X.comp_id IN (
              SELECT comp_id
              FROM glcompm
              WHERE parent_id = :compId
                 OR comp_id = :compId
                 OR grp_comp = :compId
        )
  )
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

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId ?? "";
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new RegionPivFromOtherCCModel
                        {
                            C6 = reader["C6"].ToString(),
                            Dept_Id = reader["dept_id"].ToString(),
                            Piv_No = reader["piv_no"].ToString(),
                            Piv_Receipt_No = reader["piv_receipt_no"].ToString(),
                            Piv_Date = reader["piv_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["piv_date"]),
                            Paid_Date = reader["paid_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["paid_date"]),
                            Payment_Mode = reader["payment_mode"].ToString(),
                            Cheque_No = reader["cheque_no"].ToString(),
                            Grand_Total = reader["grand_total"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["grand_total"]),
                            C8 = reader["c8"].ToString(),
                            Amount = reader["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["amount"]),
                            Bank_Check_No = reader["bank_check_no"].ToString(),
                            CCT_NAME = reader["CCT_NAME"].ToString(),
                            CCT_NAME1 = reader["CCT_NAME1"].ToString()
                        };

                        result.Add(item);
                    }
                }
            }

            return result;
        }
    }
}