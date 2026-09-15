using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MISReports_Api.DAL
{
    public class WorkInProgressCompletedCostCenterwiseRepository
    {
        public async Task<List<WorkInProgressCompletedCostCenterwiseModel>> GetWorkInProgressCompletedCostCenterwise(string costctr, string fromDate, string toDate)
        {
            var resultList = new List<WorkInProgressCompletedCostCenterwiseModel>();
            Exception lastException = null;

            Debug.WriteLine($"GetWorkInProgressCompletedCostCenterwise started for costctr: {costctr}, fromDate: {fromDate}, toDate: {toDate}");

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
       T1.project_no,
       T1.std_cost,
       T1.descr,
       T1.fund_id,
       (SELECT SUBSTR(chrg_gl_cd, 8, 5)
          FROM pcjbtypm
         WHERE dept_id = T1.dept_id
           AND cat_cd = T1.cat_cd
           AND ROWNUM = 1) AS Account_code,
       T1.cat_cd,
       T1.dept_id,
       (SELECT dept_nm
          FROM gldeptm
         WHERE dept_id = :costctr
           AND ROWNUM = 1) AS CCT_NAME,
       CASE
           WHEN T2.res_type IS NULL OR T2.res_type LIKE '%MAT%' THEN 'MAT'
           WHEN T2.res_type LIKE '%LAB%' THEN 'LAB'
           ELSE 'OTH'
       END AS c8,
       SUM(T2.commited_cost) AS commited_cost,
       c.paid_date,
       c.piv_receipt_no,
       c.piv_no,
       c.piv_amount,
       T1.conf_dt
FROM   pcesthmt T1
       LEFT OUTER JOIN piv_detail c
            ON TRIM(c.reference_type) = 'EST'
           AND TRIM(c.status) IN ('C', 'P')
           AND TRIM(T1.estimate_no) = TRIM(c.reference_no)
       JOIN pcestdmt T2
            ON T2.estimate_no = T1.estimate_no
           AND T2.dept_id = T1.dept_id
WHERE  T1.dept_id = :costctr
  AND  T1.status = 3
  AND  T1.conf_dt >= TO_DATE(:fromDate, 'yyyy/mm/dd')
  AND  T1.conf_dt <= TO_DATE(:toDate, 'yyyy/mm/dd')
  AND  T2.commited_cost IS NOT NULL
GROUP BY
       T1.project_no,
       T1.fund_id,
       T1.descr,
       T1.std_cost,
       T1.cat_cd,
       T1.dept_id,
       T2.res_type,
       c.paid_date,
       c.piv_receipt_no,
       c.piv_no,
       T1.conf_dt,
       c.piv_amount
ORDER BY
       T1.project_no,
       c.paid_date";

                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.BindByName = true;
                            cmd.Parameters.Add("costctr", costctr);
                            cmd.Parameters.Add("fromDate", fromDate);
                            cmd.Parameters.Add("toDate", toDate);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    resultList.Add(new WorkInProgressCompletedCostCenterwiseModel
                                    {
                                        ProjectNo = SafeGetString(reader, "project_no"),
                                        StdCost = SafeGetDecimal(reader, "std_cost"),
                                        Descr = SafeGetString(reader, "descr"),
                                        FundId = SafeGetString(reader, "fund_id"),
                                        AccountCode = SafeGetString(reader, "Account_code"),
                                        CatCd = SafeGetString(reader, "cat_cd"),
                                        DeptId = SafeGetString(reader, "dept_id"),
                                        CctName = SafeGetString(reader, "CCT_NAME"),
                                        C8 = SafeGetString(reader, "c8"),
                                        CommitedCost = SafeGetDecimal(reader, "commited_cost"),
                                        PaidDate = SafeGetDate(reader, "paid_date"),
                                        PivReceiptNo = SafeGetString(reader, "piv_receipt_no"),
                                        PivNo = SafeGetString(reader, "piv_no"),
                                        PivAmount = SafeGetDecimal(reader, "piv_amount"),
                                        ConfDt = SafeGetDate(reader, "conf_dt")
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
            try { int idx = reader.GetOrdinal(columnName); return reader.IsDBNull(idx) ? null : reader.GetString(idx); }
            catch { return null; }
        }

        private decimal SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            try { int idx = reader.GetOrdinal(columnName); return reader.IsDBNull(idx) ? 0 : Convert.ToDecimal(reader.GetValue(idx)); }
            catch { return 0; }
        }

        private DateTime? SafeGetDate(OracleDataReader reader, string columnName)
        {
            try { int idx = reader.GetOrdinal(columnName); return reader.IsDBNull(idx) ? (DateTime?)null : reader.GetDateTime(idx); }
            catch { return null; }
        }
    }
}