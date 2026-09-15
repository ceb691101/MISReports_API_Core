using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class JobEstimateDetailsCCDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            return reader.GetValue(ordinal)?.ToString();
        }

        private static decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            OracleDecimal od = reader.GetOracleDecimal(ordinal);
            try
            {
                od = OracleDecimal.SetPrecision(od, 28);
                return od.Value;
            }
            catch (OverflowException)
            {
                return (decimal)(double)od;
            }
        }

        private static DateTime? SafeGetDate(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            return Convert.ToDateTime(reader.GetValue(ordinal));
        }

        public List<JobEstimateDetailsCCModel> GetJobEstimateDetailsCC(DateTime fromDate, DateTime toDate, string costCtr, string matCode)
        {
            var result = new List<JobEstimateDetailsCCModel>();

            const string query = @"
                SELECT    T2.estimate_no,
                          T2.std_cost,
                          T2.project_no,
                          T2.prj_ass_dt,
                          T2.descr,
                          T1.res_cd,
                          T1.estimate_qty,
                          (SELECT mat_nm FROM inmatm WHERE mat_cd = T1.res_cd) AS MT_NM,
                          (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME,
                          (CASE WHEN T2.Status = 1                          THEN 'OPEN'
                                WHEN T2.Status = 3                          THEN 'TRANSFERED'
                                WHEN T2.Status = 6                          THEN 'TO BE APPROVED (CONSTRUCTION REVISED JOBS)'
                                WHEN T2.Status IN (5, 7)                    THEN 'UNDER-REVISION'
                                WHEN T2.Status IN (4)                       THEN 'SOFT-CLOSE'
                                WHEN T2.Status = 19                         THEN 'EXISTING JOB ENTRY'
                                WHEN T2.Status = 22                         THEN 'TO BE ALLOCATED TO CONTRACTOR'
                                WHEN T2.Status IN (55, 56, 57, 58, 59, 61)  THEN 'TO BE APPROVED (DEPOT REVISED JOBS)'
                                WHEN T2.Status IN (60)                      THEN 'REVISED JOB APPROVED.CONSUMER SHOULD BE PAY EXTRA AMOUNT'
                                ELSE 'UNKNOWN'
                           END) AS STATUS
                FROM      pcesthmt T2
                JOIN      pcestdmt T1 ON TRIM(T2.estimate_no) = TRIM(T1.estimate_no)
                                     AND TRIM(T2.dept_id) = TRIM(T1.dept_id)
                WHERE     TRIM(T2.dept_id) = TRIM(:costctr)
                  AND     T2.prj_ass_dt >= :fromdate
                  AND     T2.prj_ass_dt <  :todateexcl
                  AND     T1.res_cd LIKE :matcode || '%'
                  AND     T1.res_cat = 1
                ORDER BY  T2.estimate_no, T1.res_cd";

            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string matCodeTrimmed = (matCode ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used multiple times) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });
                cmd.Parameters.Add(new OracleParameter("matcode", OracleDbType.Varchar2) { Value = matCodeTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new JobEstimateDetailsCCModel
                        {
                            EstimateNo = SafeGetString(reader, "ESTIMATE_NO"),
                            StdCost = SafeGetDecimal(reader, "STD_COST"),
                            ProjectNo = SafeGetString(reader, "PROJECT_NO"),
                            PrjAssDt = SafeGetDate(reader, "PRJ_ASS_DT"),
                            Descr = SafeGetString(reader, "DESCR"),
                            ResCd = SafeGetString(reader, "RES_CD"),
                            EstimateQty = SafeGetDecimal(reader, "ESTIMATE_QTY"),
                            MtNm = SafeGetString(reader, "MT_NM"),
                            CctName = SafeGetString(reader, "CCT_NAME"),
                            Status = SafeGetString(reader, "STATUS")
                        });
                    }
                }
            }

            return result;
        }
    }
}