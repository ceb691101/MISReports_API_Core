using MISReports_Api.Models.SRP;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL.SRP
{
    public class AreaWiseSRPApplicationPIVStatusRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<AreaWiseSRPApplicationPIVStatusModel> GetAreaWiseSRPApplicationPIVStatusReport(string compId, DateTime fromDate, DateTime toDate)
        {
            var result = new List<AreaWiseSRPApplicationPIVStatusModel>();

            string sql = @"
SELECT a.dept_id,
       a.Id_no,
       a.application_no,
       (b.first_name || ' ' || b.last_name) AS Name,
       (b.street_address || ' ' || b.suburb || ' ' || b.city) AS address,
       a.submit_date,
       (CASE
            WHEN c.status = 'A' THEN 'To be Paid'
            WHEN c.status = 'P' THEN 'Paid'
            WHEN c.status = 'Q' THEN 'Cheque Payment'
            ELSE c.status
        END) AS status,
       a.description,
       c.Piv_no,
       c.piv_date AS Paid_date,
       c.Piv_amount,
       d.tariff_code,
       d.phase,
       d.existing_acc_no,
       (SELECT comp_id  FROM gldeptm WHERE dept_id = a.dept_id) AS Area,
       (SELECT parent_id FROM glcompm WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = a.dept_id)) AS province,
       (SELECT dept_nm  FROM gldeptm WHERE dept_id = a.dept_id) AS CCT_NAME,
       (SELECT comp_nm  FROM glcompm WHERE comp_id = :compId) AS COMP_NM
FROM   applications a,
       applicant b,
       wiring_land_detail d,
       piv_detail c
WHERE  b.Id_no = a.Id_no
AND    a.dept_id = c.dept_id
AND    TRIM(a.application_no) = TRIM(c.reference_no)
AND    a.application_id = d.application_id
AND    a.dept_id = d.dept_id
AND    a.status NOT IN ('D')
AND    a.application_type = 'CR'
AND    application_sub_type = 'RS'
AND    c.reference_type = 'APP'
AND    a.dept_id IN (
           SELECT dept_id
           FROM   gldeptm
           WHERE  status = 2
           AND    comp_id IN (
                      SELECT comp_id
                      FROM   glcompm
                      WHERE  status = 2
                      AND    comp_id = :compId
                  )
       )
AND    c.piv_date >= TO_DATE(:fromDate, 'yyyy/mm/dd')
AND    c.piv_date <= TO_DATE(:toDate, 'yyyy/mm/dd')
ORDER BY 1";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId;
                cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate.ToString("yyyy/MM/dd");
                cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate.ToString("yyyy/MM/dd");

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new AreaWiseSRPApplicationPIVStatusModel
                        {
                            Dept_Id = reader["dept_id"].ToString(),
                            Id_No = reader["Id_no"].ToString(),
                            Application_No = reader["application_no"].ToString(),
                            Name = reader["Name"].ToString(),
                            Address = reader["address"].ToString(),
                            Submit_Date = reader["submit_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["submit_date"]),
                            Status = reader["status"].ToString(),
                            Description = reader["description"].ToString(),
                            Piv_No = reader["Piv_no"].ToString(),
                            Paid_Date = reader["Paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Paid_date"]),
                            Piv_Amount = reader["Piv_amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Piv_amount"]),
                            Tariff_Code = reader["tariff_code"].ToString(),
                            Phase = reader["phase"].ToString(),
                            Existing_Acc_No = reader["existing_acc_no"].ToString(),
                            Area = reader["Area"].ToString(),
                            Province = reader["province"].ToString(),
                            Cct_Name = reader["CCT_NAME"].ToString(),
                            Comp_Nm = reader["COMP_NM"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}
