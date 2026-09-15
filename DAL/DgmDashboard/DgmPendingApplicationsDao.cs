using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.DgmDashboard;

namespace MISReports_Api.DAL.DgmDashboard
{
    public class DgmPendingApplicationsDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public List<DgmPendingApplicationModel> Fetch(int year, string companyId)
        {
            var result = new List<DgmPendingApplicationModel>();

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        app.dept_id,
                        appty.description,
                        app.application_type,
                        app.application_no
                    FROM applications app
                    INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                    INNER JOIN pcesthmt T1 ON TRIM(T1.estimate_no) = TRIM(app.application_no)
                    INNER JOIN spodrcrd L ON TRIM(T1.project_no) = TRIM(L.project_no)
                    WHERE app.status NOT IN ('D')
                    AND T1.status = 1
                    AND TO_CHAR(app.submit_date, 'YYYY') = :year
                    AND app.dept_id IN (
                        SELECT dept_id 
                        FROM gldeptm 
                        WHERE comp_id IN (
                            SELECT comp_id 
                            FROM glcompm 
                            WHERE TRIM(comp_id) = :companyId1 OR TRIM(parent_id) = :companyId2
                        )
                    )
                    ORDER BY app.dept_id, app.application_no";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("year", year.ToString()));
                    cmd.Parameters.Add(new OracleParameter("companyId1", companyId));
                    cmd.Parameters.Add(new OracleParameter("companyId2", companyId));

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new DgmPendingApplicationModel
                            {
                                deptId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim(),
                                description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                appType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                applicationNo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim()
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}