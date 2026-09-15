using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class ProvinceSetOffReportRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvinceSetOffModel> GetProvinceSetOffReport(
            DateTime fromDate,
            DateTime toDate,
            string compId)
        {
            var result = new List<ProvinceSetOffModel>();

            string sql = @"
select c.dept_id ,
       c.piv_no as s_piv_no,
       c.piv_date as s_piv_date,
       c.piv_amount as s_piv_amount,
       a.account_code as s_account_code,
       a.amount as s_account_amount,
       c.SETOFF_FROM as o_piv_no,
       c1.piv_date as o_piv_date,
       c1.piv_amount as o_piv_amount,
       a1.account_code as o_account_code,
       a1.amount as o_account_amount,
       ( select comp_nm from glcompm where trim(comp_id) = :compId ) as comp_NM
from piv_amount a ,piv_amount a1 ,piV_detail c , piV_detail c1
where c.status in ('T','M')
and C.piv_Date >= TO_DATE ( :fromDate , 'yyyy/mm/dd' )
and C.piv_Date <= TO_DATE ( :todate ,'yyyy/mm/dd' )
AND c.dept_Id in ( select dept_id
                   from gldeptm
                   where comp_id IN ( select comp_id
                                      from glcompm
                                      where trim(parent_id) = :compId or trim(comp_id) = :compId ))
and c.PIV_NO = a.PIV_NO
and a.dept_id = c.dept_id
and a1.amount != 0
and a.amount !=0
and c1.PIV_NO = a1.PIV_NO
and a1.dept_id = c1.dept_id
and c.SETOFF_FROM = c1.piv_no
order by c.dept_id ,c.piv_no";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("todate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId ?? "";

                try
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ProvinceSetOffModel
                            {
                                DeptId = reader["dept_id"]?.ToString(),
                                SPivNo = reader["s_piv_no"]?.ToString(),
                                SPivDate = reader["s_piv_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["s_piv_date"]),
                                SPivAmount = reader["s_piv_amount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["s_piv_amount"]),
                                SAccountCode = reader["s_account_code"]?.ToString(),
                                SAccountAmount = reader["s_account_amount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["s_account_amount"]),

                                OPivNo = reader["o_piv_no"]?.ToString(),
                                OPivDate = reader["o_piv_date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["o_piv_date"]),
                                OPivAmount = reader["o_piv_amount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["o_piv_amount"]),
                                OAccountCode = reader["o_account_code"]?.ToString(),
                                OAccountAmount = reader["o_account_amount"] == DBNull.Value ? null : (decimal?)Convert.ToDecimal(reader["o_account_amount"]),

                                CompNm = reader["comp_NM"]?.ToString()
                            };

                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching Province Set-off Report: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}