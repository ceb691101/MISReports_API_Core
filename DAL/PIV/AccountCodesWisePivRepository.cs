//20. PIV Details (Issued and Paid Cost Centers AFMHQ Only)

using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class AccountCodesWisePivRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<AccountCodesWisePivModel> GetAccountCodesWisePivReport(
            DateTime fromDate,
            DateTime toDate,
            string costctr)
        {
            var result = new List<AccountCodesWisePivModel>();

            // Your original SQL – unchanged
            string sql = @"
select distinct
    c.dept_id,
    c.piv_no,
    c.piv_receipt_no,
    c.piv_date,
    c.paid_date,
    a.account_code,
    a.amount,
    (select dept_nm from gldeptm where dept_id = c.dept_id) AS CCT_NAME,
    (select dept_nm from gldeptm where dept_id = :costctr) AS CCT_NAME1
from piv_amount a, piv_detail c
where c.PIV_NO = a.PIV_NO
  and a.dept_id = c.dept_id
  and trim(c.status) in ('Q', 'P','F','FR','FA')
  and c.paid_dept_id = :costctr
  and c.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  and c.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
  and c.dept_id in (select dept_id from gldeptm where comp_id = 'AFMHQ')
  and a.amount != 0
group by c.dept_id, a.account_code, c.piv_no, c.piv_receipt_no, c.piv_date, c.paid_date, a.amount
order by c.dept_id, a.account_code, c.piv_no, c.piv_receipt_no, c.piv_date, a.amount
";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costctr ?? "";

                try
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new AccountCodesWisePivModel
                            {
                                DeptId = reader["dept_id"]?.ToString(),
                                PivNo = reader["piv_no"]?.ToString(),
                                PivReceiptNo = reader["piv_receipt_no"]?.ToString(),
                                PivDate = reader["piv_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("piv_date")),
                                PaidDate = reader["paid_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("paid_date")),
                                AccountCode = reader["account_code"]?.ToString(),
                                Amount = reader["amount"] == DBNull.Value ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("amount")),
                                CctName = reader["CCT_NAME"]?.ToString(),
                                CctName1 = reader["CCT_NAME1"]?.ToString()
                            };

                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching Account Codes Wise PIV data: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}