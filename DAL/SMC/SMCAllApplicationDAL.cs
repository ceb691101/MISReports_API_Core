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
    public class SMCAllApplicationDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

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
        private static string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal)) return null;

            Type fieldType = reader.GetFieldType(ordinal);
            if (fieldType == typeof(decimal))
            {
                return SafeGetDecimal(reader, columnName)?.ToString();
            }

            return reader.GetValue(ordinal)?.ToString();
        }

        public List<SMCAllApplicationModel> GetSMCAllApplication(DateTime fromDate, DateTime toDate, string compId)
        {
            var result = new List<SMCAllApplicationModel>();

            const string query = @"
                SELECT  a.dept_id,
                        a.Id_no,
                        a.application_no,
                        (b.first_name || ' ' || b.last_name) AS Name,
                        (b.street_address || ' ' || b.suburb || ' ' || b.city) AS address,
                        a.submit_date,
                        a.description,
                        c.Piv_no,
                        c.Paid_date,
                        c.Piv_amount,
                        d.tariff_code,
                        d.phase,
                        j.project_no,
                        acc.account_no,
                        (SELECT comp_id FROM gldeptm WHERE dept_id = a.dept_id) AS Area,
                        (SELECT parent_id
                           FROM glcompm
                          WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = a.dept_id)) AS province,
                        (SELECT dept_nm FROM gldeptm WHERE dept_id = a.dept_id) AS CCT_NAME,
                        (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS COMP_NM
                FROM      applications a, applicant b, wiring_land_detail d, spexpjob acc,
                          pcesthmt j, piv_detail c
                WHERE     b.Id_no = a.Id_no
                  AND     TRIM(j.project_no) = TRIM(acc.project_no)
                  AND     TRIM(a.application_no) = TRIM(j.estimate_no)
                  AND     a.dept_id = c.dept_id
                  AND     TRIM(a.application_no) = TRIM(c.reference_no)
                  AND     a.application_id = d.application_id
                  AND     a.dept_id = d.dept_id
                  AND     a.application_type = 'NC'
                  AND     c.status IN ('P', 'C')
                  AND     c.reference_type IN ('EST', 'ELN')
                  AND     a.dept_id IN (SELECT dept_id
                                           FROM gldeptm
                                          WHERE status = 2
                                            AND TRIM(comp_id) = TRIM(:compid))
                  AND     j.prj_ass_dt >= :fromdate
                  AND     j.prj_ass_dt <  :todateexcl
                ORDER BY 1";

            string compIdTrimmed = (compId ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :compid (used twice) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compIdTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new SMCAllApplicationModel
                        {
                            DeptId = SafeGetString(reader, "dept_id"),
                            IdNo = SafeGetString(reader, "Id_no"),
                            ApplicationNo = SafeGetString(reader, "application_no"),
                            Name = SafeGetString(reader, "Name"),
                            Address = SafeGetString(reader, "address"),
                            SubmitDate = reader["submit_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["submit_date"]),
                            Description = SafeGetString(reader, "description"),
                            PivNo = SafeGetString(reader, "Piv_no"),
                            PaidDate = reader["Paid_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Paid_date"]),
                            PivAmount = SafeGetDecimal(reader, "Piv_amount"),
                            TariffCode = SafeGetString(reader, "tariff_code"),
                            Phase = SafeGetString(reader, "phase"),
                            ProjectNo = SafeGetString(reader, "project_no"),
                            AccountNo = SafeGetString(reader, "account_no"),
                            Area = SafeGetString(reader, "Area"),
                            Province = SafeGetString(reader, "province"),
                            CctName = SafeGetString(reader, "CCT_NAME"),
                            CompNm = SafeGetString(reader, "COMP_NM")
                        });
                    }
                }
            }

            return result;
        }
    }
}