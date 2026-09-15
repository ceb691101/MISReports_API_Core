using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class JobSearchMainTableRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public async Task<List<JobSearchMainTable>> SearchAsync(
            string applicationId = null,
            string applicationNo = null,
            string projectNo = null,
            string idNo = null,
            string accountNo = null,
            string telephone = null)
        {
            var results = new List<JobSearchMainTable>();

            try
            {
                using (var conn = new OracleConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string sql = @"
SELECT ar.application_id,
       ar.application_no,
       ar.id_no,
       ar.projectno,
       a.first_name,
       a.last_name,
       a.STREET_ADDRESS,
       a.SUBURB,
       a.CITY,
       t.description AS application_type_desc,
       MAX(wld.dept_id) AS dept_id,
       MAX(wld.existing_acc_no) AS existing_acc_no,
       app.telephone_no AS Tel,
       app.mobile_no AS mobile,
       ap.submit_date
FROM application_reference ar
LEFT JOIN applications ap
       ON ar.application_id = ap.application_id
LEFT JOIN applicationtypes t
       ON t.apptype = ap.application_type
LEFT JOIN wiring_land_detail wld
       ON ar.application_id = wld.application_id
LEFT JOIN applicant a
       ON a.id_no = ar.id_no
LEFT JOIN applicant app
       ON ar.id_no = app.id_no
WHERE (
      (:application_id IS NOT NULL AND ar.application_id = :application_id)
   OR (:application_no IS NOT NULL AND ar.application_no = :application_no)
   OR (:projectno IS NOT NULL AND ar.projectno = :projectno)
   OR (:idno IS NOT NULL AND ar.id_no = :idno)
   OR (:accountNo IS NOT NULL AND (
            ar.application_id IN (
                SELECT application_id
                FROM wiring_land_detail
                WHERE existing_acc_no = :accountNo
            )
         OR ar.projectno IN (
                SELECT project_no
                FROM spexpjob
                WHERE account_no = :accountNo
            )
        ))
   OR (:tele IS NOT NULL AND (app.telephone_no = :tele OR app.mobile_no = :tele))
)
GROUP BY ar.application_id,
         ar.application_no,
         ar.id_no,
         ar.projectno,
         a.first_name,
         a.last_name,
         a.STREET_ADDRESS,
         a.SUBURB,
         a.CITY,
         t.description,
         app.telephone_no,
         app.mobile_no,
         ap.submit_date
ORDER BY ar.application_id";

                    using (var cmd = new OracleCommand(sql, conn))
                    {
                        cmd.BindByName = true;

                        cmd.Parameters.Add(new OracleParameter("application_id", string.IsNullOrWhiteSpace(applicationId) ? DBNull.Value : (object)applicationId));
                        cmd.Parameters.Add(new OracleParameter("application_no", string.IsNullOrWhiteSpace(applicationNo) ? DBNull.Value : (object)applicationNo));
                        cmd.Parameters.Add(new OracleParameter("projectno", string.IsNullOrWhiteSpace(projectNo) ? DBNull.Value : (object)projectNo));
                        cmd.Parameters.Add(new OracleParameter("idno", string.IsNullOrWhiteSpace(idNo) ? DBNull.Value : (object)idNo.Trim()));
                        cmd.Parameters.Add(new OracleParameter("accountNo", string.IsNullOrWhiteSpace(accountNo) ? DBNull.Value : (object)accountNo));
                        cmd.Parameters.Add(new OracleParameter("tele", string.IsNullOrWhiteSpace(telephone) ? DBNull.Value : (object)telephone));

                        // Repeated parameter (used twice in subqueries)
                        cmd.Parameters.Add(new OracleParameter("accountNo", string.IsNullOrWhiteSpace(accountNo) ? DBNull.Value : (object)accountNo));

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new JobSearchMainTable
                                {
                                    ApplicationId = reader["application_id"] != DBNull.Value ? reader["application_id"].ToString() : null,

                                    ApplicationNo = reader["application_no"] != DBNull.Value ? reader["application_no"].ToString() : null,
                                    IdNo = reader["id_no"] != DBNull.Value ? reader["id_no"].ToString() : null,
                                    ProjectNo = reader["projectno"] != DBNull.Value ? reader["projectno"].ToString() : null,

                                    FirstName = reader["first_name"] != DBNull.Value ? reader["first_name"].ToString() : null,
                                    LastName = reader["last_name"] != DBNull.Value ? reader["last_name"].ToString() : null,
                                    StreetAddress = reader["STREET_ADDRESS"] != DBNull.Value ? reader["STREET_ADDRESS"].ToString() : null,
                                    Suburb = reader["SUBURB"] != DBNull.Value ? reader["SUBURB"].ToString() : null,
                                    City = reader["CITY"] != DBNull.Value ? reader["CITY"].ToString() : null,

                                    ApplicationTypeDesc = reader["application_type_desc"] != DBNull.Value ? reader["application_type_desc"].ToString() : null,

                                    DeptId = reader["dept_id"] != DBNull.Value ? reader["dept_id"].ToString() : null,
                                    ExistingAccNo = reader["existing_acc_no"] != DBNull.Value ? reader["existing_acc_no"].ToString() : null,

                                    TelephoneNo = reader["Tel"] != DBNull.Value ? reader["Tel"].ToString() : null,
                                    MobileNo = reader["mobile"] != DBNull.Value ? reader["mobile"].ToString() : null,

                                    SubmitDate = reader["submit_date"] != DBNull.Value ? Convert.ToDateTime(reader["submit_date"]) : (DateTime?)null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"JobSearchMainTableRepository error: {ex.Message}\n{ex.StackTrace}");
                throw;
            }

            return results;
        }
    }
}