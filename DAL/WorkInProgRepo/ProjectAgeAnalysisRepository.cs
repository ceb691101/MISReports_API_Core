using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class ProjectAgeAnalysisRepository
    {
        public async Task<List<ProjectAgeAnalysisModel>> GetProjectAgeAnalysis(string deptId)
        {
            var resultList = new List<ProjectAgeAnalysisModel>();
            Exception lastException = null;

            Debug.WriteLine($"GetProjectAgeAnalysis started for deptId: {deptId}");

            string[] connectionStringNames = { "HQOracle" };

            foreach (var connectionStringName in connectionStringNames)
            {
                try
                {
                    string connectionString = ConfigurationManager.ConnectionStrings[connectionStringName]?.ConnectionString;
                    if (string.IsNullOrEmpty(connectionString)) continue;

                    using (var conn = new OracleConnection(connectionString))
                    {
                        await conn.OpenAsync();

                        string sql = @"
                            SELECT DISTINCT
                                (CASE
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 3 THEN 'Months 0-3'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 6 THEN 'Months 3-6'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 9 THEN 'Months 6-9'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 12 THEN 'Months 9-12'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 24 THEN 'Years 1-2'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 36 THEN 'Years 2-3'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 48 THEN 'Years 3-4'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 60 THEN 'Years 4-5'
                                    ELSE 'Years 5 Over'
                                END) AS Period,
                                COUNT(T1.project_no) AS no_of_projects,
                                (SELECT dept_nm FROM gldeptm WHERE dept_id = :deptId) AS cct_name
                            FROM pcesthmt T1
                            WHERE T1.dept_id = :deptId
                              AND T1.cat_cd NOT IN ('MTN','MAIN','MAINT','MTN_TL','MTN_TL_REH','BDJ','7840','LSF','MAINTENANCE','AMU','MNT','EMU','PSF','FSM','MDR')
                              AND T1.status <> 3
                            GROUP BY
                                (CASE
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 3 THEN 'Months 0-3'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 6 THEN 'Months 3-6'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 9 THEN 'Months 6-9'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 12 THEN 'Months 9-12'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 24 THEN 'Years 1-2'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 36 THEN 'Years 2-3'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 48 THEN 'Years 3-4'
                                    WHEN ROUND(MONTHS_BETWEEN(SYSDATE, T1.prj_ass_dt), 0) <= 60 THEN 'Years 4-5'
                                    ELSE 'Years 5 Over'
                                END)
                            ORDER BY 1 ASC";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.Parameters.Add("deptId", deptId);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    resultList.Add(new ProjectAgeAnalysisModel
                                    {
                                        Period = SafeGetString(reader, "Period"),
                                        NoOfProjects = SafeGetInt(reader, "no_of_projects"),
                                        CctName = SafeGetString(reader, "cct_name")
                                    });
                                }
                            }
                        }
                        return resultList;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    continue;
                }
            }

            if (lastException != null)
                throw new Exception("All DB connections failed", lastException);

            return resultList;
        }

        private string SafeGetString(OracleDataReader reader, string columnName)
        {
            try
            {
                int idx = reader.GetOrdinal(columnName);
                return reader.IsDBNull(idx) ? null : reader.GetString(idx);
            }
            catch { return null; }
        }

        private int SafeGetInt(OracleDataReader reader, string columnName)
        {
            try
            {
                int idx = reader.GetOrdinal(columnName);
                return reader.IsDBNull(idx) ? 0 : Convert.ToInt32(reader.GetValue(idx));
            }
            catch { return 0; }
        }
    }
}