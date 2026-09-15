using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class ProjectCommitmentSummaryRepository
    {
        public async Task<List<ProjectCommitmentSummaryModel>> GetProjectCommitmentSummary(string deptId)
        {
            var resultList = new List<ProjectCommitmentSummaryModel>();
            Exception lastException = null;

            string[] connectionStringNames = { "HQOracle" };

            foreach (var connectionStringName in connectionStringNames)
            {
                try
                {
                    string connectionString = ConfigurationManager.ConnectionStrings[connectionStringName]?.ConnectionString;
                    if (string.IsNullOrEmpty(connectionString))
                        continue;

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
                               SUM(T3.commited_cost) AS Sum,
                               (SELECT dept_nm FROM gldeptm WHERE dept_id = :deptId) AS cct_name
                            FROM pcesthmt T1, pcestdmt T3
                            WHERE T1.estimate_no = T3.estimate_no
                              AND T1.dept_id = T3.dept_id
                              AND T1.dept_id = :deptId
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
                                    resultList.Add(new ProjectCommitmentSummaryModel
                                    {
                                        Period = reader.IsDBNull(reader.GetOrdinal("Period")) ? null : reader.GetString(reader.GetOrdinal("Period")),
                                        Sum = reader.IsDBNull(reader.GetOrdinal("Sum")) ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("Sum")),
                                        CctName = reader.IsDBNull(reader.GetOrdinal("cct_name")) ? null : reader.GetString(reader.GetOrdinal("cct_name"))
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
                }
            }

            if (lastException != null)
                throw new Exception("All DB connections failed", lastException);

            return resultList;
        }
    }
}