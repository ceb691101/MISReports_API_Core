using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class CostCenterWorkInProgressRepository
    {
        public async Task<List<CostCenterWorkInProgressModel>> GetCostCenterWorkInProgress(string deptId)
        {
            var workInProgressList = new List<CostCenterWorkInProgressModel>();
            Exception lastException = null;

            Debug.WriteLine($"GetCostCenterWorkInProgress method started for deptId: {deptId}");

            // Try each connection string in order
            string[] connectionStringNames = { "HQOracle" };

            foreach (var connectionStringName in connectionStringNames)
            {
                try
                {
                    Debug.WriteLine($"Trying connection: {connectionStringName}");
                    string connectionString = ConfigurationManager.ConnectionStrings[connectionStringName]?.ConnectionString;
                    if (string.IsNullOrEmpty(connectionString))
                    {
                        Debug.WriteLine($"Connection string {connectionStringName} is null or empty");
                        continue;
                    }

                    using (var conn = new OracleConnection(connectionString))
                    {
                        Debug.WriteLine("Opening connection...");
                        await conn.OpenAsync();
                        Debug.WriteLine("Connection opened successfully");

                        // First test with a simple query
                        if (!await TestSimpleQuery(conn, deptId))
                        {
                            Debug.WriteLine("Simple query test failed");
                            continue;
                        }

                        Debug.WriteLine("Executing main query...");
                        string sql = @"
                            select  (TO_CHAR(T2.prj_ass_dt,'YYYY') ) as c6,
                                    T2.project_no,
                                    T2.cat_cd,
                                    t2.descr,
                                    T2.fund_source,
                                    T3.wip_yr  as wip_yr,
                                    T3.wip_mth  as wip_mth,
                                    c.piv_no,
                                    c.grand_total,
                                    C.paid_date,
                                    (case when T2.Status in (4) THEN T2.apr_dt1 else null end ) as soft_close_date,
                                    T2.Std_cost  as Estimated_cost,
                                    (case
                                        when T2.status =1 then  'Open'
                                        when T2.status =3 then  'Closed'
                                        when T2.Status = 6 THEN 'TO BE APPROVED (CONSTRUCTION REVISED JOBS)'
                                        when T2.Status in(5, 7) THEN 'UNDER-REVISION'
                                        when T2.Status in (4) THEN 'SOFT-CLOSE'
                                        when T2.Status = 19 THEN 'EXISTING JOB ENTRY'
                                        when T2.Status = 22 THEN 'TO BE ALLOCATED TO CONTRACTOR'
                                        when T2.Status = 41 THEN 'REJECTED JOB'
                                        when T2.Status in(55,56,57,58,59,61) THEN 'TO BE APPROVED (DEPOT REVISED JOBS)'
                                        when T2.Status in(60) THEN 'REVISED JOB APPROVED.CONSUMER SHOULD BE PAY EXTRA AMOUNT'
                                        ELSE 'UNKNOWN'
                                     END) as status,
                                    (case
                                        when T1.res_type is null or T1.res_type like '%MAT%' then 'MAT'
                                        when T1.res_type like '%LAB%' then 'LAB'
                                        else 'OTHER'
                                     end) as c8,
                                   sum(T1.commited_cost) as commited_cost,
                                   (select dept_nm from gldeptm where dept_id = :deptId) AS CCT_NAME
                            from pcestdmt T1,
                                 pcwiph T3,
                                 (pcesthmt T2 LEFT OUTER JOIN piv_detail c
                                     ON trim(T2.estimate_no) = trim(c.reference_no)
                                     and c.status in ('Q','P','F','FR','FA')
                                     and c.reference_type ='EST'
                                     and c.dept_id = T2.dept_id)
                            where T2.estimate_no = T1.estimate_no
                              and T2.dept_id = T1.dept_id
                              and T2.dept_id = :deptId
                              and T2.project_no = T3.project_no
                              and T2.fund_id = T3.fund_id
                              and T2.cat_cd not in ('MTN','MAIN','MAINT','MTN_TL','MTN_TL_REH','BDJ','7840','LSF','MAINTENANCE','AMU','MNT','EMU','PSF','FSM','MDR')
                              and T2.status <> 3
                            group by T2.prj_ass_dt, T2.project_no, T2.cat_cd, T2.fund_source,
                                     T3.wip_yr, T3.wip_mth, T1.res_type, T1.commited_cost,
                                     T2.Std_cost, t2.descr, T2.estimate_no, T2.status,
                                     T2.apr_dt1, c.piv_no, c.grand_total, C.paid_date

                            union all

                            select  (TO_CHAR(T2.prj_ass_dt,'YYYY') ) as c6,
                                    T2.project_no,
                                    T2.cat_cd,
                                    t2.descr,
                                    T2.fund_source,
                                    0 as wip_yr,
                                    0 as wip_mth,
                                    c.piv_no,
                                    c.grand_total,
                                    C.paid_date,
                                    (case when T2.Status in (4) THEN T2.apr_dt1 else null end ) as soft_close_date,
                                    T2.Std_cost as Estimated_cost,
                                    (case
                                        when T2.status =1 then  'Open'
                                        when T2.status =3 then  'Closed'
                                        when T2.Status = 6 THEN 'TO BE APPROVED (CONSTRUCTION REVISED JOBS)'
                                        when T2.Status in(5, 7) THEN 'UNDER-REVISION'
                                        when T2.Status in (4) THEN 'SOFT-CLOSE'
                                        when T2.Status = 19 THEN 'EXISTING JOB ENTRY'
                                        when T2.Status = 22 THEN 'TO BE ALLOCATED TO CONTRACTOR'
                                        when T2.Status = 41 THEN 'REJECTED JOB'
                                        when T2.Status in(55,56,57,58,59,61) THEN 'TO BE APPROVED (DEPOT REVISED JOBS)'
                                        when T2.Status in(60) THEN 'REVISED JOB APPROVED.CONSUMER SHOULD BE PAY EXTRA AMOUNT'
                                        ELSE 'UNKNOWN'
                                     END) as status,
                                    (case
                                        when T1.res_type is null or T1.res_type like '%MAT%' then 'MAT'
                                        when T1.res_type like '%LAB%' then 'LAB'
                                        else 'OTHER'
                                     end) as c8,
                                   sum(T1.commited_cost) as commited_cost,
                                   (select dept_nm from gldeptm where dept_id = :deptId) AS CCT_NAME
                            from pcestdmt T1,
                                 (pcesthmt T2 LEFT OUTER JOIN piv_detail c
                                     ON trim(T2.estimate_no) = trim(c.reference_no)
                                     and c.status in ('Q','P','F','FR','FA')
                                     and c.reference_type ='EST'
                                     and c.dept_id = T2.dept_id)
                            where T2.estimate_no = T1.estimate_no
                              and T2.dept_id = T1.dept_id
                              and T2.dept_id = :deptId
                              and trim(T2.cat_cd) not in ('MTN','MAIN','MAINT','MTN_TL','MTN_TL_REH','BDJ','7840','LSF','MAINTENANCE','AMU','MNT','EMU','PSF','FSM','MDR')
                              and T2.status <> 3
                              and T2.project_no not in (select project_no from pcwiph)
                            group by T2.prj_ass_dt, T2.project_no, T2.cat_cd, T2.fund_source,
                                     T1.res_type, T1.commited_cost, T2.Std_cost, t2.descr, T2.estimate_no,
                                     T2.status, T2.apr_dt1, c.piv_no, c.grand_total, C.paid_date
                            order by 1 desc";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.CommandTimeout = 180; // 3 minutes timeout
                            cmd.Parameters.Add("deptId", deptId);

                            Debug.WriteLine("Executing main query...");
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                Debug.WriteLine("Reading data from query...");
                                int recordCount = 0;

                                while (await reader.ReadAsync())
                                {
                                    workInProgressList.Add(new CostCenterWorkInProgressModel
                                    {
                                        AssignmentYear = SafeGetString(reader, "c6"),
                                        ProjectNo = SafeGetString(reader, "project_no"),
                                        CategoryCode = SafeGetString(reader, "cat_cd"),
                                        Description = SafeGetString(reader, "descr"),
                                        FundSource = SafeGetString(reader, "fund_source"),
                                        WipYear = SafeGetInt(reader, "wip_yr"),
                                        WipMonth = SafeGetInt(reader, "wip_mth"),
                                        PivNo = SafeGetString(reader, "piv_no"),
                                        GrandTotal = SafeGetDecimal(reader, "grand_total"),
                                        PaidDate = SafeGetDateTime(reader, "paid_date"),
                                        SoftCloseDate = SafeGetDateTime(reader, "soft_close_date"),
                                        EstimatedCost = SafeGetDecimal(reader, "Estimated_cost"),
                                        Status = SafeGetString(reader, "status"),
                                        ResourceType = SafeGetString(reader, "c8"),
                                        CommittedCost = SafeGetDecimal(reader, "commited_cost") ?? 0,
                                        CctName = SafeGetString(reader, "CCT_NAME")
                                    });

                                    recordCount++;
                                    if (recordCount % 100 == 0)
                                    {
                                        Debug.WriteLine($"Processed {recordCount} records...");
                                    }
                                }

                                Debug.WriteLine($"Query completed. Total records: {recordCount}");
                            }
                        }

                        Debug.WriteLine("Returning successful result");
                        return workInProgressList;
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Debug.WriteLine($"Failed to connect using {connectionStringName}: {ex.Message}");
                    // Continue to try next connection string
                }
            }

            // If all connections failed, throw the last exception
            if (lastException != null)
            {
                Debug.WriteLine($"All connection attempts failed: {lastException.Message}");
                throw new Exception("All database connection attempts failed", lastException);
            }

            Debug.WriteLine("No successful connections found");
            return workInProgressList;
        }

        private async Task<bool> TestSimpleQuery(OracleConnection conn, string deptId)
        {
            try
            {
                Debug.WriteLine("Testing with simple query...");
                string simpleSql = "SELECT :deptId as test_value FROM DUAL";

                using (var cmd = new OracleCommand(simpleSql, conn))
                {
                    cmd.BindByName = true;
                    cmd.CommandTimeout = 30;
                    cmd.Parameters.Add("deptId", deptId);

                    var result = await cmd.ExecuteScalarAsync();
                    Debug.WriteLine($"Simple query test passed: {result}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Simple query test failed: {ex.Message}");
                return false;
            }
        }

        // Helper methods for safe data conversion
        private string SafeGetString(OracleDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private int SafeGetInt(OracleDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal)) return 0;

                object value = reader.GetValue(ordinal);
                if (value is decimal) return Convert.ToInt32((decimal)value);
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal);
            }
            catch
            {
                return null;
            }
        }

        private DateTime? SafeGetDateTime(OracleDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
            }
            catch
            {
                return null;
            }
        }
    }
}