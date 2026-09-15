//12. PIV Search
// File: PivSearchRepository.cs
using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class PivSearchRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PivSearchModel> GetPivDetails(string piv = null, string project = null)
        {
            var result = new List<PivSearchModel>();

            // At least one parameter should be provided
            if (string.IsNullOrWhiteSpace(piv) && string.IsNullOrWhiteSpace(project))
            {
                return result;
            }

            string sql = @"
select c.piv_no,
       c.reference_no,
       to_char(c.cheque_no) as cheque_no,
       c.paid_amount,
       c.piv_date,
       c.paid_date as paid_date,
       c.paid_dept_id,
       (Case when c.payment_mode='C' then 'Cash'
             when c.payment_mode='Q' then 'Cheque'
             else c.payment_mode end ) as Payment_mode
from piv_detail c
where c.piv_No = :piv
  and c.piv_no not in (select c1.piv_no from piv_payment c1 where c1.piv_no is not null)
union all
select c.piv_no,
       c1.reference_no,
       c.cheque_no as cheque_no,
       c.paid_amount,
       c1.piv_date,
       c.add_date as paid_date,
       c1.paid_dept_id,
       (Case when c.payment_mode='C' then 'Cash'
             when c.payment_mode='Q' then 'Cheque'
             else c.payment_mode end ) as Payment_mode
from piv_payment c, piv_detail c1
where c.piv_No = c1.piv_No
  and c.piv_No = :piv
union all
select c.piv_no,
       c.reference_no,
       to_char(c.cheque_no) as cheque_no,
       c.paid_amount,
       c.piv_date,
       c.paid_date as paid_date,
       c.paid_dept_id,
       (Case when c.payment_mode='C' then 'Cash'
             when c.payment_mode='Q' then 'Cheque'
             else c.payment_mode end ) as Payment_mode
from piv_detail c
where trim(c.reference_no) = :project
  and c.piv_no not in (select c1.piv_no from piv_payment c1 where c1.piv_no is not null)
union all
select c.piv_no,
       c1.reference_no,
       c.cheque_no as cheque_no,
       c.paid_amount,
       c1.piv_date,
       c.add_date as paid_date,
       c1.paid_dept_id,
       (Case when c.payment_mode='C' then 'Cash'
             when c.payment_mode='Q' then 'Cheque'
             else c.payment_mode end ) as Payment_mode
from piv_payment c, piv_detail c1
where c.piv_No = c1.piv_No
  and trim(c1.reference_no) = :project";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;

                // Bind parameters - use DBNull.Value when parameter is not provided
                cmd.Parameters.Add("piv", OracleDbType.Varchar2).Value =
                    string.IsNullOrWhiteSpace(piv) ? (object)DBNull.Value : piv.Trim();

                cmd.Parameters.Add("project", OracleDbType.Varchar2).Value =
                    string.IsNullOrWhiteSpace(project) ? (object)DBNull.Value : project.Trim();

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new PivSearchModel
                        {
                            Piv_No = reader["piv_no"].ToString(),
                            Reference_No = reader["reference_no"].ToString(),
                            Cheque_No = reader["cheque_no"].ToString(),
                            Paid_Amount = reader["paid_amount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["paid_amount"]),
                            Piv_Date = reader["piv_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["piv_date"]),
                            Paid_Date = reader["paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["paid_date"]),
                            Paid_Dept_Id = reader["paid_dept_id"].ToString(),
                            Payment_Mode = reader["Payment_mode"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}