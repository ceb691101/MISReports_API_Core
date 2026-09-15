using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class RegionWiseVatReportRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<RegionWiseVatReportModel> GetRegionWiseVatReport(
            DateTime fromDate,
            DateTime toDate,
            string compId)
        {
            var result = new List<RegionWiseVatReportModel>();

            string sql = @"
select distinct
    a.name ,
    T1.title_cd,
    (Case when T1.description is null then (select title_nm from gltitlm where title_cd=T1.title_cd) else T1.description end ) as description,
    (select title_nm from gltitlm where title_cd=T1.title_cd) as piv_type,
    substr (a.vat_reg_no,0,9)as VAT_NO,
    T1.paid_date as piv_date ,
    T1.piv_no ,
    T2.amount as VAT_AMT ,
    T1.PIV_amount,
    (select comp_nm from glcompm where comp_id = :compId ) as comp_nm
from piv_detail T1, piv_amount T2, piv_applicant a
where T1.piv_no=T2.piv_no
and T2.account_code='L5225'
and T1.PIV_NO= a.PIV_NO
and trim(T1.status) in ('Q', 'P','F','FR','FA')
and T1.paid_Date >= TO_DATE(:fromDate,'yyyy/mm/dd') and
       T1.paid_Date<=TO_DATE(:toDate,'yyyy/mm/dd')
 AND T1.dept_Id in ( select dept_id
                                       from gldeptm
                                       where comp_id IN ( select comp_id
                                                          from glcompm
                                                          where parent_id = :compId or comp_id = :compId or grp_comp = :compId))
and T2.amount > 0
order by T1.piv_no, T1.paid_date";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId ?? "";

                try
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new RegionWiseVatReportModel
                            {
                                Name = reader["name"]?.ToString(),
                                TitleCd = reader["title_cd"]?.ToString(),
                                Description = reader["description"]?.ToString(),
                                PivType = reader["piv_type"]?.ToString(),
                                VatNo = reader["VAT_NO"]?.ToString(),
                                PivDate = reader["piv_date"] == DBNull.Value
                                                    ? (DateTime?)null
                                                    : Convert.ToDateTime(reader["piv_date"]),
                                PivNo = reader["piv_no"]?.ToString(),
                                VatAmt = reader["VAT_AMT"] == DBNull.Value
                                                    ? (decimal?)null
                                                    : Convert.ToDecimal(reader["VAT_AMT"]),
                                PivAmount = reader["PIV_amount"] == DBNull.Value
                                                    ? (decimal?)null
                                                    : Convert.ToDecimal(reader["PIV_amount"]),
                                CompNm = reader["comp_nm"]?.ToString()
                            };
                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: proper logging in real project
                    throw new Exception("Error fetching Region Wise VAT report: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}