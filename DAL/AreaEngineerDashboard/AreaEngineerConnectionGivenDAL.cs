using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.DgmDashboard;

namespace MISReports_Api.DAL.AreaEngineerDashboard
{
    public class AreaEngineerConnectionGivenDAL
    {
        private static readonly string ConnectionString = System.Configuration.ConfigurationManager
            .ConnectionStrings["HQOracle"].ConnectionString;

        public List<DgmConnectionGivenModel> Fetch(int year, string companyId)
        {
            var result = new List<DgmConnectionGivenModel>();

            using (OracleConnection conn = new OracleConnection(ConnectionString))
            {
                conn.Open();
                string query = @"
                    SELECT 
                        app.dept_id,
                        appty.description,
                        app.application_type,
                        COUNT(*) AS no_of_connections
                        FROM applications app
                        INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                        INNER JOIN pcesthmt T1 ON TRIM(T1.estimate_no) = TRIM(app.application_no)
                        INNER JOIN spodrcrd L ON TRIM(T1.project_no) = TRIM(L.project_no)
                        WHERE app.status NOT IN ('D') 
                        AND TO_CHAR(app.submit_date, 'YYYY') = :year
                        AND app.dept_id IN (
                            SELECT dept_id 
                            FROM gldeptm 
                            WHERE comp_id  = :companyId
                        )
                        GROUP BY app.dept_id, appty.description, app.application_type
                        union all
                        SELECT 
                            app.dept_id,
                            appty.description,
                            app.application_type,
                            COUNT(*) AS no_of_connections
                        FROM applications app
                        INNER JOIN applicationtypes appty ON app.application_type = appty.apptype
                        INNER JOIN pcesthmt T1 ON TRIM(T1.estimate_no) = TRIM(app.application_no)
                        WHERE app.status NOT IN ('D') 
                        AND TO_CHAR(app.submit_date, 'YYYY') = :year and T1. Status   in (3,4)
                        and  TRIM(T1.project_no) not in( select TRIM(L.project_no)  from spodrcrd L  )
                        AND app.dept_id IN (
                            SELECT dept_id 
                            FROM gldeptm 
                            WHERE comp_id = :companyId
                        )
                        GROUP BY app.dept_id, appty.description, app.application_type
                        ORDER BY 1";

                using (OracleCommand cmd = new OracleCommand(query, conn))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("year", year.ToString()));
                    cmd.Parameters.Add(new OracleParameter("companyId", companyId));

                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new DgmConnectionGivenModel
                            {
                                deptId = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim(),
                                description = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim(),
                                appType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                                noOfConnections = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3))
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}