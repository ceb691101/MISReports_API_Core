//26. C/C PIV Details (Status Report)

using MISReports_Api.Models.PIV;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.PIV
{
    public class CostCenterwisePivdetailsRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CostCenterwisePivdetailsModel> GetCostCenterPivDetails(
            string costCenter,
            DateTime fromDate,
            DateTime toDate)
        {
            var result = new List<CostCenterwisePivdetailsModel>();

            string sql = @"
SELECT DISTINCT
    c.dept_id,
    c.piv_no,
    c.piv_date,
    c.paid_date,
    c.payment_mode,
    c.grand_total AS piv_amount,
    (SELECT description_1
     FROM piv_activity
     WHERE trim(activity_code) = trim(c.status)) AS status,
    (SELECT dept_nm
     FROM gldeptm
     WHERE dept_id = c.dept_id) AS CCT_NAME,
    (SELECT dept_nm
     FROM gldeptm
     WHERE dept_id = '') AS CCT_NAME1
FROM piv_detail c
WHERE
    c.dept_id = :costctr
    AND c.piv_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
    AND c.piv_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
ORDER BY
    c.dept_id, c.piv_no
";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("costctr", OracleDbType.Varchar2).Value = costCenter?.Trim() ?? "";
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new CostCenterwisePivdetailsModel
                            {
                                DeptId = reader["dept_id"]?.ToString(),
                                PivNo = reader["piv_no"]?.ToString(),
                                PivDate = reader["piv_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("piv_date")),
                                PaidDate = reader["paid_date"] == DBNull.Value ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("paid_date")),
                                PaymentMode = reader["payment_mode"]?.ToString(),
                                PivAmount = reader["piv_amount"] == DBNull.Value ? null : (decimal?)reader.GetDecimal(reader.GetOrdinal("piv_amount")),
                                Status = reader["status"]?.ToString(),
                                CctName = reader["CCT_NAME"]?.ToString(),
                                CctName1 = reader["CCT_NAME1"]?.ToString()     // usually empty — consider removing if always null
                            };
                            result.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error fetching Cost Center wise PIV details: " + ex.Message, ex);
                }
            }

            return result;
        }
    }
}