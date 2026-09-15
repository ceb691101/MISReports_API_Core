//22. Refunded PIV Details
using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class RefundedPivRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<RefundedPivModel> GetRefundedPivs(
            DateTime fromDate,
            DateTime toDate,
            string costctr)
        {
            var result = new List<RefundedPivModel>();

            string sql = @"
SELECT
    c.dept_id,
    (SELECT title_nm FROM gltitlm WHERE title_cd = c.title_cd) AS title_cd,
    c.piv_date,
    c.paid_date,
    c.piv_no,
    a.name,
    a.address,
    c.description,
    c.grand_total,
    r.refundable_amount,
    r.refund_date,
    p.account_code,
    (SELECT dept_nm FROM gldeptm WHERE dept_id = :costctr) AS cct_name
FROM
    piv_applicant a,
    piv_detail   c,
    piv_amount   p,
    piv_refund   r
WHERE
    c.PIV_NO = a.PIV_NO
    AND c.PIV_NO = p.PIV_NO
    AND c.PIV_NO = r.PIV_NO
    AND TRIM(c.status) IN ('F')
    AND c.dept_id = :costctr
    AND r.refund_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
    AND r.refund_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
ORDER BY
    c.dept_id, c.title_cd, c.piv_no
";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costctr ?? "";

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new RefundedPivModel
                            {
                                DeptId = reader["dept_id"]?.ToString(),
                                TitleCd = reader["title_cd"]?.ToString(),
                                PivDate = reader["piv_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("piv_date")),
                                PaidDate = reader["paid_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("paid_date")),
                                PivNo = reader["piv_no"]?.ToString(),
                                Name = reader["name"]?.ToString(),
                                Address = reader["address"]?.ToString(),
                                Description = reader["description"]?.ToString(),
                                GrandTotal = reader["grand_total"] == DBNull.Value ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("grand_total")),
                                RefundableAmount = reader["refundable_amount"] == DBNull.Value ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("refundable_amount")),
                                RefundDate = reader["refund_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("refund_date")),
                                AccountCode = reader["account_code"]?.ToString(),
                                CctName = reader["cct_name"]?.ToString()
                            };

                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching Refunded PIV data: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}