using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class PIVDetailsRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PIVDetailsReportModel> GetPIVDetailsReport(DateTime fromDate, DateTime toDate)
        {
            var result = new List<PIVDetailsReportModel>();

            string sql = @"
select distinct
    C.dept_id,
    c.paid_dept_id,
    c.piv_no,
    c.piv_receipt_no,
    c.piv_date,
    c.paid_date,
    c.payment_mode,
    c.piv_amount,
    c.paid_amount,
    (c.piv_amount - c.paid_amount) as Difference,
    c.bank_check_no,
    (select dept_nm from gldeptm where dept_id = C.dept_id) AS CCT_NAME,
    (Case when substr(c.dept_id,3,1) = '0' then
            (select comp_id from glcompm where comp_id in (select comp_id from gldeptm where dept_id = c.dept_id))
         when substr(c.dept_id,3,1) != '0' then
            (select parent_id from glcompm where comp_id in (select comp_id from gldeptm where dept_id = c.dept_id))
         else 'No Company' end) AS company
from piv_detail c
where trim(c.status) in ('Q', 'P','F','FR','FA')
    and c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
    and c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
    and c.piv_amount != c.paid_amount
group by
    c.dept_id,
    c.piv_no,
    c.piv_receipt_no,
    c.piv_date,
    c.paid_date,
    c.payment_mode,
    c.cheque_no,
    c.grand_total,
    c.bank_check_no,
    c.paid_dept_id,
    c.piv_amount,
    c.paid_amount
order by
    c.dept_id,
    c.piv_no,
    c.piv_receipt_no";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new PIVDetailsReportModel
                        {
                            Dept_Id = reader["dept_id"].ToString(),
                            Paid_Dept_Id = reader["paid_dept_id"].ToString(),
                            Piv_No = reader["piv_no"].ToString(),
                            Piv_Receipt_No = reader["piv_receipt_no"].ToString(),
                            Piv_Date = reader["piv_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv_date"]),
                            Paid_Date = reader["paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["paid_date"]),
                            Payment_Mode = reader["payment_mode"].ToString(),
                            Piv_Amount = reader["piv_amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["piv_amount"]),
                            Paid_Amount = reader["paid_amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["paid_amount"]),
                            Difference = reader["Difference"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Difference"]),
                            Bank_Check_No = reader["bank_check_no"].ToString(),
                            Cct_Name = reader["CCT_NAME"].ToString(),
                            Company = reader["company"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}