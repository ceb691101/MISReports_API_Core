using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class StampDutyDetailedRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<StampDutyDetailedModel> GetStampDutyDetailedReport(
            string compId,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<StampDutyDetailedModel>();

            string sql = @"
SELECT p.dept_id,
       p.paid_Date,
       p.piv_date,
       p.piv_no,
       p.piv_Amount as Amount,
       25 as stamp_duty,
       (select comp_nm from glcompm where comp_id = :compId) as comp_nm
FROM Piv_Detail p
WHERE p.paid_Date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  AND p.paid_Date <= TO_DATE(:toDate, 'yyyy/mm/dd')
  AND p.piv_Amount > 25000
  AND (status = 'P' OR status = 'FR' OR status = 'FA' OR status = 'F' OR status = 'Q')
  AND p.dept_Id IN (
      SELECT dept_id
      FROM gldeptm
      WHERE comp_id IN (
          SELECT comp_id
          FROM glcompm
          WHERE grp_comp = :compId
             OR parent_id = :compId
             OR comp_id = :compId
      )
  )
ORDER BY p.dept_id, p.piv_date, p.piv_no";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                try
                {
                    conn.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new StampDutyDetailedModel
                            {
                                DeptId = reader["dept_id"]?.ToString(),
                                PaidDate = reader["paid_Date"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["paid_Date"]),
                                PivDate = reader["piv_date"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["piv_date"]),
                                PivNo = reader["piv_no"]?.ToString(),
                                Amount = reader["Amount"] == DBNull.Value
                                    ? (decimal?)null
                                    : Convert.ToDecimal(reader["Amount"]),
                                StampDuty = Convert.ToInt32(reader["stamp_duty"]),
                                CompanyName = reader["comp_nm"]?.ToString()
                            };

                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // TODO: In production → log exception properly (Serilog/NLog/etc)
                    throw new Exception("Error fetching Stamp Duty Detailed report: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}