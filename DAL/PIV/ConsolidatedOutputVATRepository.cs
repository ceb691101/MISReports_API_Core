using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class ConsolidatedOutputVATRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ConsolidatedOutputVATModel> GetConsolidatedOutputVAT(
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<ConsolidatedOutputVATModel>();

            string sql = @"
select distinct
    a.name,
    T1.title_cd,
    (CASE WHEN T1.description IS NULL
          THEN (SELECT title_nm FROM gltitlm WHERE title_cd = T1.title_cd)
          ELSE T1.description END) as description,
    (SELECT title_nm FROM gltitlm WHERE title_cd = T1.title_cd) as piv_type,
    substr(a.vat_reg_no, 0, 9) as VAT_NO,
    T1.paid_date as piv_date,
    T1.piv_no,
    T2.amount as VAT_AMT,
    T1.PIV_amount
from piv_detail T1
join piv_amount T2 ON T1.piv_no = T2.piv_no
join piv_applicant a ON T1.PIV_NO = a.PIV_NO
where T2.account_code = 'L5225'
  and trim(T1.status) in ('Q', 'P', 'F', 'FR', 'FA')
  and T1.paid_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  and T1.paid_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
  and T2.amount > 0
order by T1.piv_no, T1.paid_date";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                try
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new ConsolidatedOutputVATModel
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
                                    : Convert.ToDecimal(reader["PIV_amount"])
                            };

                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // In real project you should log the error
                    throw new Exception("Error fetching Consolidated Output VAT report: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}