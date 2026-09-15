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
    public class RegionSMCAllPIVDAL
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

        public List<RegionSMCAllPIVModel> GetRegionSMCAllPIV(DateTime fromDate, DateTime toDate, string compId)
        {
            var result = new List<RegionSMCAllPIVModel>();

            const string query = @"
                SELECT   a.dept_id,
                         a.Id_no,
                         a.application_no,
                         (b.first_name || ' ' || b.last_name) AS NAME,
                         (b.street_address || ' ' || b.suburb || ' ' || b.city) AS ADDRESS,
                         a.submit_date,
                         a.description,
                         c.Piv_no,
                         c.Paid_date,
                         c.Piv_amount,
                         d.tariff_code,
                         d.phase,
                         TO_CHAR(c.cheque_no)  AS CHEQUE_NO,
                         ''                     AS CHEQUE_NO1,
                         (SELECT comp_id FROM gldeptm WHERE dept_id = a.dept_id) AS AREA,
                         (SELECT parent_id FROM glcompm WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = a.dept_id)) AS PROVINCE,
                         (SELECT dept_nm FROM gldeptm WHERE dept_id = a.dept_id) AS CCT_NAME,
                         (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS COMP_NM
                FROM      applications a
                JOIN      applicant b ON b.Id_no = a.Id_no
                JOIN      wiring_land_detail d ON a.application_id = d.application_id
                                               AND TRIM(a.dept_id) = TRIM(d.dept_id)
                JOIN      piv_detail c ON c.Id_no = a.Id_no
                                      AND TRIM(a.dept_id) = TRIM(c.dept_id)
                                      AND TRIM(a.application_no) = TRIM(c.reference_no)
                WHERE     a.application_type = 'NC'
                  AND     a.application_sub_type NOT IN ('EA')
                  AND     c.status IN ('P', 'Q')
                  AND     c.piv_no NOT IN (SELECT piv.piv_no FROM piv_payment piv WHERE piv.dept_id = a.dept_id)
                  AND     c.reference_type = 'EST'
                  AND     a.DEPT_ID IN
                          (SELECT dept_id
                             FROM gldeptm
                            WHERE status = 2
                              AND comp_id IN
                                  (SELECT comp_id
                                     FROM glcompm
                                    WHERE status = 2
                                      AND (TRIM(comp_id) = TRIM(:compid)
                                        OR TRIM(parent_id) = TRIM(:compid)
                                        OR TRIM(grp_comp) = TRIM(:compid))))
                  AND     c.Paid_date >= :fromdate
                  AND     c.Paid_date <  :todateexcl

                UNION ALL

                SELECT   a.dept_id,
                         a.Id_no,
                         a.application_no,
                         (b.first_name || ' ' || b.last_name) AS NAME,
                         (b.street_address || ' ' || b.suburb || ' ' || b.city) AS ADDRESS,
                         a.submit_date,
                         a.description,
                         c.Piv_no,
                         c.Paid_date,
                         c.Piv_amount,
                         d.tariff_code,
                         d.phase,
                         '0'                    AS CHEQUE_NO,
                         TO_CHAR(piv.cheque_no) AS CHEQUE_NO1,
                         (SELECT comp_id FROM gldeptm WHERE dept_id = a.dept_id) AS AREA,
                         (SELECT parent_id FROM glcompm WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = a.dept_id)) AS PROVINCE,
                         (SELECT dept_nm FROM gldeptm WHERE dept_id = a.dept_id) AS CCT_NAME,
                         (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS COMP_NM
                FROM      applications a
                JOIN      applicant b ON b.Id_no = a.Id_no
                JOIN      wiring_land_detail d ON a.application_id = d.application_id
                                               AND TRIM(a.dept_id) = TRIM(d.dept_id)
                JOIN      piv_detail c ON c.Id_no = a.Id_no
                                      AND TRIM(a.dept_id) = TRIM(c.dept_id)
                                      AND TRIM(a.application_no) = TRIM(c.reference_no)
                JOIN      piv_payment piv ON piv.piv_no = c.piv_no
                WHERE     a.application_type = 'NC'
                  AND     a.application_sub_type NOT IN ('EA')
                  AND     c.status IN ('P', 'Q')
                  AND     c.reference_type = 'EST'
                  AND     a.DEPT_ID IN
                          (SELECT dept_id
                             FROM gldeptm
                            WHERE status = 2
                              AND comp_id IN
                                  (SELECT comp_id
                                     FROM glcompm
                                    WHERE status = 2
                                      AND (TRIM(comp_id) = TRIM(:compid)
                                        OR TRIM(parent_id) = TRIM(:compid)
                                        OR TRIM(grp_comp) = TRIM(:compid))))
                  AND     c.Paid_date >= :fromdate
                  AND     c.Paid_date <  :todateexcl

                ORDER BY 1";

            string compIdTrimmed = (compId ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :compid/:fromdate/:todateexcl (each used multiple times) bind correctly by name

                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compIdTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new RegionSMCAllPIVModel
                        {
                            DeptId = SafeGetString(reader, "DEPT_ID"),
                            IdNo = SafeGetString(reader, "ID_NO"),
                            ApplicationNo = SafeGetString(reader, "APPLICATION_NO"),
                            Name = SafeGetString(reader, "NAME"),
                            Address = SafeGetString(reader, "ADDRESS"),
                            SubmitDate = SafeGetDate(reader, "SUBMIT_DATE"),
                            Description = SafeGetString(reader, "DESCRIPTION"),
                            PivNo = SafeGetString(reader, "PIV_NO"),
                            PaidDate = SafeGetDate(reader, "PAID_DATE"),
                            PivAmount = SafeGetDecimal(reader, "PIV_AMOUNT"),
                            TariffCode = SafeGetString(reader, "TARIFF_CODE"),
                            Phase = SafeGetString(reader, "PHASE"),
                            ChequeNo = SafeGetString(reader, "CHEQUE_NO"),
                            ChequeNo1 = SafeGetString(reader, "CHEQUE_NO1"),
                            Area = SafeGetString(reader, "AREA"),
                            Province = SafeGetString(reader, "PROVINCE"),
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