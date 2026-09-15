using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class DivisionWiseSRPEstimationRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<DivisionWiseSRPEstimationModel>> GetDivisionWiseSRP(
            string compId, string fromDate, string toDate)
        {
            var result = new List<DivisionWiseSRPEstimationModel>();

            compId = compId.Trim().ToUpper();

            using (var conn = new OracleConnection(_connectionString))
            {
                await conn.OpenAsync();

                string sql = @"
SELECT 
    gc.grp_comp AS Division,
    gc.parent_id AS Province,
    gd.comp_id AS Area,
    gd.dept_nm AS CCT_NAME,
    gcr.comp_nm AS comp_nm,

    a.dept_id,
    a.id_no,
    a.application_no,

    (b.first_name || ' ' || b.last_name) AS Name,
    (b.street_address || ' ' || b.suburb || ' ' || b.city) AS address,

    a.submit_date,
    a.description,

    c.piv_no,
    c.paid_date,
    c.piv_amount,

    d.tariff_code,
    d.phase,
    d.existing_acc_no

FROM applications a

JOIN applicant b 
    ON b.id_no = a.id_no

JOIN piv_detail c 
    ON a.dept_id = c.dept_id

JOIN wiring_land_detail d 
    ON a.application_id = d.application_id 
   AND a.dept_id = d.dept_id

JOIN application_reference app 
    ON a.application_no = app.application_no

JOIN gldeptm gd 
    ON gd.dept_id = a.dept_id

JOIN glcompm gc 
    ON gc.comp_id = gd.comp_id

JOIN glcompm gcr 
    ON gcr.comp_id = :compId

WHERE 
    (
        a.application_no = c.reference_no
        OR app.projectno = c.reference_no
    )

AND a.application_type = 'CR'
AND a.application_sub_type = 'RS'
AND c.reference_type IN ('EST','JOB')

AND gd.status = 2
AND gc.status = 2

-- 🔥 FAST FILTER (EXISTS instead of IN)
AND EXISTS (
    SELECT 1
    FROM glcompm g2
    WHERE g2.status = 2
    AND (g2.comp_id = :compId 
         OR g2.parent_id = :compId 
         OR g2.grp_comp = :compId)
    AND g2.comp_id = gd.comp_id
)

-- 🔥 DATE FILTER (NO FUNCTION ON COLUMN → INDEX FRIENDLY)
AND c.piv_date >= TO_DATE(:fromDate,'yyyy/mm/dd')
AND c.piv_date < TO_DATE(:toDate,'yyyy/mm/dd') + 1

ORDER BY 
    gc.grp_comp,
    gc.parent_id,
    gd.comp_id,
    a.dept_id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.BindByName = true;

                    cmd.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId;
                    cmd.Parameters.Add("fromDate", OracleDbType.Varchar2).Value = fromDate;
                    cmd.Parameters.Add("toDate", OracleDbType.Varchar2).Value = toDate;

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new DivisionWiseSRPEstimationModel
                            {
                                Division = reader["Division"]?.ToString(),
                                Province = reader["Province"]?.ToString(),
                                Area = reader["Area"]?.ToString(),
                                CctName = reader["CCT_NAME"]?.ToString(),
                                CompName = reader["comp_nm"]?.ToString(),

                                DeptId = reader["dept_id"]?.ToString(),
                                IdNo = reader["id_no"]?.ToString(),
                                ApplicationNo = reader["application_no"]?.ToString(),
                                Name = reader["Name"]?.ToString(),
                                Address = reader["address"]?.ToString(),

                                SubmitDate = reader["submit_date"] != DBNull.Value
                                    ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("submit_date"))
                                    : null,

                                Description = reader["description"]?.ToString(),
                                PivNo = reader["piv_no"]?.ToString(),

                                PaidDate = reader["paid_date"] != DBNull.Value
                                    ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("paid_date"))
                                    : null,

                                PivAmount = reader["piv_amount"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["piv_amount"]) : 0,

                                TariffCode = reader["tariff_code"]?.ToString(),
                                Phase = reader["phase"]?.ToString(),
                                ExistingAccNo = reader["existing_acc_no"]?.ToString()
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}