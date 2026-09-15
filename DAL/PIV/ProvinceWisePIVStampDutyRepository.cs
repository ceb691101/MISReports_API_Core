// 09.Province wise PIV Stamp Duty
// File: ProvinceWisePIVStampDutyRepository.cs

using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class ProvinceWisePIVStampDutyRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvinceWisePIVStampDutyModel> GetProvinceWisePIVStampDutyReport(
            string compID, DateTime fromDate, DateTime toDate)
        {
            var result = new List<ProvinceWisePIVStampDutyModel>();

            string sql = @"
SELECT
    p.paid_Date,
    'PIV' as Pay_type,
    count(p.piv_No) as count,
    sum(p.piv_Amount) as Amount,
    count(p.piv_No)*25 as stamp_duty,
    (select comp_nm from glcompm where trim(comp_id) = :compID ) as comp_nm
FROM Piv_Detail p
WHERE p.paid_Date >= TO_DATE( :fromDate,'yyyy/mm/dd')
  and p.paid_Date <= TO_DATE( :toDate ,'yyyy/mm/dd')
  AND p.piv_Amount > 25000
  AND (status = 'P' or status = 'FR' or status = 'FA' or status = 'F' or status='Q')
  AND p.dept_Id in (
      select dept_id
      from gldeptm
      where comp_id IN (
          select comp_id
          from glcompm
          where trim(parent_id) = :compID or trim(comp_id) = :compID
      )
  )
group by p.paid_Date
ORDER BY p.paid_Date";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("compID", OracleDbType.Varchar2).Value = compID;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new ProvinceWisePIVStampDutyModel
                        {
                            Paid_Date = reader["paid_Date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["paid_Date"]),
                            Pay_type = reader["Pay_type"].ToString(),
                            Count = Convert.ToInt32(reader["count"]),
                            Amount = reader["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Amount"]),
                            Stamp_Duty = reader["stamp_duty"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["stamp_duty"]),
                            Comp_nm = reader["comp_nm"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }
            return result;
        }
    }
}