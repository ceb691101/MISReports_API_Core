//07.PIV Collections by Peoples Banks

// File: Repositories/PivByPeopleBankRepository.cs
using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.Repositories
{
    public class PivByPeopleBankRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<PivByPeopleBankModel> GetPivByPeopleBankReport(DateTime fromDate, DateTime toDate)
        {
            var result = new List<PivByPeopleBankModel>();

            string sql = @"
               select distinct
    c.dept_id as Cost_center_ID ,
    a.account_code as Account_Code ,
        sum(a.amount) as amount,
        (select dept_nm from gldeptm
        where dept_id =  c.dept_id ) AS Cost_center_Name ,
        ( Case when substr(c.dept_id ,3,1)= '0' then
            (select comp_id from glcompm
                where comp_id in (select comp_id from gldeptm
                where dept_id  =  c.dept_id ) )
                when  substr(c.dept_id ,3,1) != '0' then (select parent_id from glcompm
                        where comp_id in (select comp_id from gldeptm
                        where dept_id =  c.dept_id ))
             else 'No Company' end ) AS Company_name
             from piv_amount a ,  piv_detail c
    where   c.PIV_NO = a.PIV_NO
    and a.dept_id = c.dept_id
    and trim( c.status ) in ('Q', 'P','F','FR','FA')
    and c.paid_dept_id =   '000.00'
    and c.paid_agent =   '7135'
    and c.paid_date >=  TO_DATE( :fromDate,'yyyy/mm/dd' )
    and c.paid_date <=    TO_DATE( :toDate,'yyyy/mm/dd' )
group by
    c.dept_id ,
    a.account_code
order by
    c.dept_id ,
    a.account_code";

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
                        var model = new PivByPeopleBankModel
                        {
                            Cost_center_ID = reader["Cost_center_ID"].ToString(),
                            Account_Code = reader["Account_Code"].ToString(),
                            Amount = reader["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["amount"]),
                            Cost_center_Name = reader["Cost_center_Name"].ToString(),
                            Company_name = reader["Company_name"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}