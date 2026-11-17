using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using MISReports_Api.Models.SolarJobs;

namespace MISReports_Api.DAL.SolarJobs
{
    public class SolarOrdinaryCustomersGenerationCapacityDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<SolarOrdinaryCustomersGenerationCapacityModel>
            GetSolarOrdinaryCustomersGenerationCapacity(
                DateTime fromDate,
                DateTime toDate)
        {
            var result =
                new List<SolarOrdinaryCustomersGenerationCapacityModel>();

            const string query = @"
                SELECT 
                    'DD1' AS Division,
                    COUNT(b.job_no) AS No_of_accounts,
                    SUM(b.CAPACITY) AS Generated_capacity
                FROM BANKING_DETAILS b, SPODRCRD s 
                WHERE s.CONNECTED_DATE >= :fromdate
                  AND s.CONNECTED_DATE < :todateexcl
                  AND TRIM(s.project_no) = TRIM(b.job_no)
                  AND b.APPLICATION_SUBTYPE IN ('NM','NA','NP','PP')
                  AND b.dept_id IN
                  (
                      SELECT d.dept_id
                      FROM gldeptm d
                      WHERE d.status = 2
                        AND d.comp_id IN
                        (
                            SELECT comp_id
                            FROM glcompm
                            WHERE comp_id = 'DISCO1'
                               OR parent_id = 'DISCO1'
                               OR grp_comp = 'DISCO1'
                        )
                  )

                UNION ALL

                SELECT 
                    'DD2' AS Division,
                    COUNT(b.job_no) AS No_of_accounts,
                    SUM(b.CAPACITY) AS Generated_capacity
                FROM BANKING_DETAILS b, SPODRCRD s 
                WHERE s.CONNECTED_DATE >= :fromdate
                  AND s.CONNECTED_DATE < :todateexcl
                  AND TRIM(s.project_no) = TRIM(b.job_no)
                  AND b.APPLICATION_SUBTYPE IN ('NM','NA','NP','PP')
                  AND b.dept_id IN
                  (
                      SELECT d.dept_id
                      FROM gldeptm d
                      WHERE d.status = 2
                        AND d.comp_id IN
                        (
                            SELECT comp_id
                            FROM glcompm
                            WHERE comp_id = 'DISCO2'
                               OR parent_id = 'DISCO2'
                               OR grp_comp = 'DISCO2'
                        )
                  )

                UNION ALL

                SELECT 
                    'DD3' AS Division,
                    COUNT(b.job_no) AS No_of_accounts,
                    SUM(b.CAPACITY) AS Generated_capacity
                FROM BANKING_DETAILS b, SPODRCRD s 
                WHERE s.CONNECTED_DATE >= :fromdate
                  AND s.CONNECTED_DATE < :todateexcl
                  AND TRIM(s.project_no) = TRIM(b.job_no)
                  AND b.APPLICATION_SUBTYPE IN ('NM','NA','NP','PP')
                  AND b.dept_id IN
                  (
                      SELECT d.dept_id
                      FROM gldeptm d
                      WHERE d.status = 2
                        AND d.comp_id IN
                        (
                            SELECT comp_id
                            FROM glcompm
                            WHERE comp_id = 'DISCO3'
                               OR parent_id = 'DISCO3'
                               OR grp_comp = 'DISCO3'
                        )
                  )

                UNION ALL

                SELECT 
                    'DD4' AS Division,
                    COUNT(b.job_no) AS No_of_accounts,
                    SUM(b.CAPACITY) AS Generated_capacity
                FROM BANKING_DETAILS b, SPODRCRD s 
                WHERE s.CONNECTED_DATE >= :fromdate
                  AND s.CONNECTED_DATE < :todateexcl
                  AND TRIM(s.project_no) = TRIM(b.job_no)
                  AND b.APPLICATION_SUBTYPE IN ('NM','NA','NP','PP')
                  AND b.dept_id IN
                  (
                      SELECT d.dept_id
                      FROM gldeptm d
                      WHERE d.status = 2
                        AND d.comp_id IN
                        (
                            SELECT comp_id
                            FROM glcompm
                            WHERE comp_id = 'DISCO4'
                               OR parent_id = 'DISCO4'
                               OR grp_comp = 'DISCO4'
                        )
                  )

                ORDER BY Division";

            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(
                    new OracleParameter("fromdate", OracleDbType.Date)
                    {
                        Value = fromDate.Date
                    });

                cmd.Parameters.Add(
                    new OracleParameter("todateexcl", OracleDbType.Date)
                    {
                        Value = toDateExclusive
                    });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(
                            new SolarOrdinaryCustomersGenerationCapacityModel
                            {
                                Division =
                                    reader["Division"] == DBNull.Value
                                        ? null
                                        : reader["Division"].ToString(),

                                NoOfAccounts =
                                    reader["No_of_accounts"] == DBNull.Value
                                        ? 0
                                        : Convert.ToInt32(
                                            reader["No_of_accounts"]),

                                GeneratedCapacity =
                                    reader["Generated_capacity"] == DBNull.Value
                                        ? (decimal?)null
                                        : Convert.ToDecimal(
                                            reader["Generated_capacity"])
                            });
                    }
                }
            }

            return result;
        }
    }
}