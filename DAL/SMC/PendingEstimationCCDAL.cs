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
    public class PendingEstimationCCDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;
        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            Type fieldType = reader.GetFieldType(ordinal);
            if (fieldType == typeof(decimal))
            {
                OracleDecimal od = reader.GetOracleDecimal(ordinal);
                try
                {
                    od = OracleDecimal.SetPrecision(od, 28);
                    return od.Value.ToString();
                }
                catch (OverflowException)
                {
                    return ((double)od).ToString();
                }
            }

            return reader.GetValue(ordinal)?.ToString();
        }

        private static DateTime? SafeGetDate(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;
            return Convert.ToDateTime(reader.GetValue(ordinal));
        }

        public List<PendingEstimationCCModel> GetPendingEstimationCC(DateTime fromDate, DateTime toDate, string costCtr, string jobType)
        {
            var result = new List<PendingEstimationCCModel>();

            string jobTypeFilter = string.IsNullOrWhiteSpace(jobType)
                ? "1 = 1"
                : "a.APPLICATION_TYPE = :jobtype";

            string query = @"
                SELECT    a.APPLICATION_TYPE,
                          (SELECT DISTINCT Description
                             FROM applicationsubtypes
                            WHERE AppSUBTYPE = a.application_sub_type) AS APPLICATION_SUB_TYPE,
                          a.APPLICATION_NO,
                          a.APPLICATION_ID,
                          (b.first_name || ' ' || b.last_name) AS NAME,
                          (b.street_address || ' ' || b.suburb || ' ' || b.city) AS CUS_ADDRESS,
                          a.SUBMIT_DATE,
                          a.STATUS,
                          d.TARIFF_CODE,
                          d.PHASE,
                          d.CONNECTION_TYPE,
                          (SELECT dept_nm FROM gldeptm WHERE TRIM(dept_id) = TRIM(:costctr)) AS CCT_NAME
                FROM      APPLICATIONS a
                JOIN      APPLICANT b ON b.Id_no = a.Id_no
                JOIN      WIRING_LAND_DETAIL d ON a.application_id = d.application_id
                                               AND TRIM(a.dept_id) = TRIM(d.dept_id)
                WHERE     a.STATUS NOT IN ('E', 'D')
                  AND     TRIM(a.dept_id) = TRIM(:costctr)
                  AND     a.SUBMIT_DATE >= :fromdate
                  AND     a.SUBMIT_DATE <  :todateexcl
                  AND     " + jobTypeFilter + @"
                ORDER BY  a.APPLICATION_TYPE ASC, APPLICATION_SUB_TYPE ASC, a.APPLICATION_NO ASC";

            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :costctr (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                if (!string.IsNullOrWhiteSpace(jobType))
                    cmd.Parameters.Add(new OracleParameter("jobtype", OracleDbType.Varchar2) { Value = jobType.Trim() });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new PendingEstimationCCModel
                        {
                            ApplicationType = SafeGetString(reader, "APPLICATION_TYPE"),
                            ApplicationSubType = SafeGetString(reader, "APPLICATION_SUB_TYPE"),
                            ApplicationNo = SafeGetString(reader, "APPLICATION_NO"),
                            ApplicationId = SafeGetString(reader, "APPLICATION_ID"),
                            Name = SafeGetString(reader, "NAME"),
                            CusAddress = SafeGetString(reader, "CUS_ADDRESS"),
                            SubmitDate = SafeGetDate(reader, "SUBMIT_DATE"),
                            Status = SafeGetString(reader, "STATUS"),
                            TariffCode = SafeGetString(reader, "TARIFF_CODE"),
                            Phase = SafeGetString(reader, "PHASE"),
                            ConnectionType = SafeGetString(reader, "CONNECTION_TYPE"),
                            CctName = SafeGetString(reader, "CCT_NAME")
                        });
                    }
                }
            }

            return result;
        }
    }
}