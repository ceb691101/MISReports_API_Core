using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.DgmDashboard;

namespace MISReports_Api.DAL.DgmDashboard
{
    public class DgmAppCountDao
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public List<DgmAppCountModel> Fetch(int year, string companyId)
        {
            var result = new List<DgmAppCountModel>();

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        app.dept_id,
                        appty.description,
                        app.application_type,
                        COUNT(*) AS no_of_applications
                    FROM applications app
                    INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                    WHERE app.status NOT IN ('D')
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
                    GROUP BY app.dept_id, appty.description, app.application_type
                    ORDER BY app.dept_id";

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
                            result.Add(new DgmAppCountModel
                            {
                                deptId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim(),
                                description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                appType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                noOfApplications = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3))
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}
