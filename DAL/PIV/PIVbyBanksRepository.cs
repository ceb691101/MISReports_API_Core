using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class PIVbyBanksRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PIVbyBanksModel> GetPIVbyBanksReport(DateTime fromDate, DateTime toDate)
        {
            var result = new List<PIVbyBanksModel>();

            string sql = @"
               select distinct
    c.dept_id as Issued_cost_center ,
    a.account_code as Account_code ,
    sum(a.amount) as amount  ,
(select dept_nm from gldeptm where dept_id   =  c.dept_id ) AS Issued_cost_center_name,
( Case when substr (c.dept_id ,3,1) = '0' then (select comp_id from glcompm where comp_id in  ( select comp_id from gldeptm where dept_id   =  c.dept_id ))
       when  substr ( c.dept_id ,3,1) != '0' then (select parent_id from glcompm where comp_id in (select comp_id from gldeptm where dept_id =  c.dept_id ))
       else 'No Company' end ) AS company_name
 from piv_amount a ,  piv_detail c
where   c.PIV_NO = a.PIV_NO
and a.dept_id = c.dept_id
and trim(c.status) in ('Q', 'P','F','FR','FA')
and c.paid_dept_id =   '000.00'
and c.paid_date >=  TO_DATE( :fromDate ,'yyyy/mm/dd')
and c.paid_date <=    TO_DATE( :toDate,'yyyy/mm/dd')
group by  c.dept_id ,a.account_code
order by c.dept_id ,a.account_code";

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
                        var model = new PIVbyBanksModel
                        {
                            Issued_cost_center = reader["Issued_cost_center"].ToString(),
                            Account_code = reader["Account_code"].ToString(),
                            Amount = reader["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["amount"]),
                            Company_name = reader["company_name"].ToString(),
                            Issued_cost_center_name = reader["Issued_cost_center_name"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}