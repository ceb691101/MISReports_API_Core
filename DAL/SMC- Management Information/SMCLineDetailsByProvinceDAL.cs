using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class SMCLineDetailsByProvinceDAL
    {
        // DTO returned to the Branch (Province) dropdown.
        public class CompanyDropdownItem
        {
            public string CompId { get; set; }
            public string CompName { get; set; }
        }

        private readonly string _connectionString;

        public SMCLineDetailsByProvinceDAL()
        {
            ConnectionStringSettings settings =
                ConfigurationManager.ConnectionStrings["HQOracle"];

            if (settings == null ||
                string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "The 'HQOracle' connection string is missing or empty in Web.config.");
            }

            _connectionString = settings.ConnectionString;
        }

        private static string SafeGetString(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
                return null;

            object value = reader.GetValue(ordinal);

            if (value is OracleDecimal oracleDecimal)
            {
                try
                {
                    return oracleDecimal.Value.ToString(
                        CultureInfo.InvariantCulture);
                }
                catch (OverflowException)
                {
                    return ((double)oracleDecimal).ToString(
                        CultureInfo.InvariantCulture);
                }
            }

            return Convert.ToString(
                value,
                CultureInfo.InvariantCulture);
        }

        private static decimal? SafeGetDecimal(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
                return null;

            OracleDecimal oracleDecimal =
                reader.GetOracleDecimal(ordinal);

            try
            {
                return oracleDecimal.Value;
            }
            catch (OverflowException)
            {
                return Convert.ToDecimal(
                    (double)oracleDecimal,
                    CultureInfo.InvariantCulture);
            }
        }

        private static DateTime? SafeGetDateTime(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
                return null;

            return reader.GetDateTime(ordinal);
        }

        // ============================================================
        // GET ACTIVE COMPANIES FOR THE SMC LINE DETAILS DROPDOWN
        // ============================================================
        // This method is separate from GetCompaniesByUserlevel because
        // the existing Usercompanies endpoint is shared by other reports.
        //
        // IMPORTANT:
        // This returns ALL active companies (STATUS = 2).
        // Apply the application's required authorization rules before
        // exposing this list in a production environment.
        // ============================================================
        public List<CompanyDropdownItem> GetActiveCompanies()
        {
            const string query = @"
                SELECT
                    TRIM(comp_id) AS COMP_ID,
                    TRIM(comp_nm) AS COMP_NM
                FROM glcompm
                WHERE status = 2
                  AND comp_id IS NOT NULL
                ORDER BY comp_nm";

            var result = new List<CompanyDropdownItem>();

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;

                conn.Open();

                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CompanyDropdownItem
                        {
                            CompId =
                                SafeGetString(reader, "COMP_ID"),

                            CompName =
                                SafeGetString(reader, "COMP_NM")
                        });
                    }
                }
            }

            return result;
        }

        // ============================================================
        // GET SMC LINE DETAILS BY PROVINCE / COMPANY
        // ============================================================
        public List<SMCLineDetailsByProvinceModel>
            GetSMCLineDetailsByProvince(
                string compId,
                string fromDate,
                string toDate,
                string wiringType)
        {
            if (string.IsNullOrWhiteSpace(compId))
            {
                throw new ArgumentException(
                    "Company/province ID is required.",
                    nameof(compId));
            }

            if (!DateTime.TryParse(
                    fromDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fromDt))
            {
                throw new ArgumentException(
                    "fromDate must be a valid date.",
                    nameof(fromDate));
            }

            if (!DateTime.TryParse(
                    toDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime toDt))
            {
                throw new ArgumentException(
                    "toDate must be a valid date.",
                    nameof(toDate));
            }

            fromDt = fromDt.Date;
            toDt = toDt.Date;

            if (toDt < fromDt)
            {
                throw new ArgumentException(
                    "toDate cannot be earlier than fromDate.");
            }

            string compIdTrimmed = compId.Trim();

            string wiringTypeTrimmed =
                string.IsNullOrWhiteSpace(wiringType) ||
                string.Equals(
                    wiringType.Trim(),
                    "ALL",
                    StringComparison.OrdinalIgnoreCase)
                    ? null
                    : wiringType.Trim().ToUpperInvariant();

            // ALL returns every wiring type.
            // Otherwise, filter using the selected database value.
            string wiringTypeCondition =
                wiringTypeTrimmed == null
                    ? string.Empty
                    : " AND UPPER(TRIM(d.wiring_type)) = :wiringtype ";

            string query = @"
                SELECT
                    a.dept_id AS DEPT_ID,
                    b.phase AS PHASE,
                    b.connection_type AS CONNECTION_TYPE,
                    b.tariff_cat_code AS TARIFF_CAT_CODE,
                    e.loop_cable AS LOOP_CABLE,
                    d.wiring_type AS WIRING_TYPE,
                    a.prj_ass_dt AS PRJ_ASS_DT,
                    a.project_no AS PROJECT_NO,
                    d.line_length AS LINE_LENGTH,
                    a.std_cost AS ACTUAL_COST,
                    d.total_cost AS STANDARD_COST,
                    (
                        SELECT MAX(gc.comp_nm)
                        FROM glcompm gc
                        WHERE TRIM(gc.comp_id) = TRIM(:compid)
                    ) AS COMP_NM
                FROM pcesthmt a
                JOIN application_reference c
                    ON TRIM(a.estimate_no) = TRIM(c.application_no)
                JOIN wiring_land_detail b
                    ON TRIM(b.application_id) = TRIM(c.application_id)
                JOIN speststd d
                    ON TRIM(a.estimate_no) = TRIM(d.estimate_no)
                JOIN spserest e
                    ON TRIM(e.application_no) = TRIM(c.application_no)
                WHERE a.prj_ass_dt >= :fromdate
                  AND a.prj_ass_dt < :todateexcl
                  AND a.estimate_no LIKE '%ENC%'
                  " + wiringTypeCondition + @"
                  AND a.dept_id IN
                  (
                      SELECT gd.dept_id
                      FROM gldeptm gd
                      WHERE TRIM(gd.comp_id) IN
                      (
                          SELECT gc2.comp_id
                          FROM glcompm gc2
                          WHERE gc2.status = 2
                            AND
                            (
                                TRIM(gc2.comp_id) = TRIM(:compid)
                                OR TRIM(gc2.parent_id) = TRIM(:compid)
                                OR TRIM(gc2.grp_comp) = TRIM(:compid)
                            )
                      )
                  )
                GROUP BY
                    a.dept_id,
                    b.phase,
                    b.connection_type,
                    b.tariff_cat_code,
                    e.loop_cable,
                    d.wiring_type,
                    a.prj_ass_dt,
                    a.project_no,
                    d.line_length,
                    a.std_cost,
                    d.total_cost,
                    c.application_no
                ORDER BY
                    a.dept_id,
                    b.phase,
                    b.connection_type,
                    b.tariff_cat_code,
                    e.loop_cable,
                    d.wiring_type";

            DateTime toDateExclusive = toDt.AddDays(1);

            var result =
                new List<SMCLineDetailsByProvinceModel>();

            using (var conn =
                new OracleConnection(_connectionString))
            using (var cmd =
                new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(
                    "compid",
                    OracleDbType.Varchar2,
                    50).Value = compIdTrimmed;

                cmd.Parameters.Add(
                    "fromdate",
                    OracleDbType.Date).Value = fromDt;

                cmd.Parameters.Add(
                    "todateexcl",
                    OracleDbType.Date).Value = toDateExclusive;

                if (wiringTypeTrimmed != null)
                {
                    cmd.Parameters.Add(
                        "wiringtype",
                        OracleDbType.Varchar2,
                        20).Value = wiringTypeTrimmed;
                }

                conn.Open();

                using (OracleDataReader reader =
                    cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(
                            new SMCLineDetailsByProvinceModel
                            {
                                DeptId =
                                    SafeGetString(reader, "DEPT_ID"),

                                Phase =
                                    SafeGetString(reader, "PHASE"),

                                ConnectionType =
                                    SafeGetString(reader, "CONNECTION_TYPE"),

                                TariffCatCode =
                                    SafeGetString(reader, "TARIFF_CAT_CODE"),

                                LoopCable =
                                    SafeGetString(reader, "LOOP_CABLE"),

                                WiringType =
                                    SafeGetString(reader, "WIRING_TYPE"),

                                PrjAssDt =
                                    SafeGetDateTime(reader, "PRJ_ASS_DT"),

                                ProjectNo =
                                    SafeGetString(reader, "PROJECT_NO"),

                                LineLength =
                                    SafeGetString(reader, "LINE_LENGTH"),

                                ActualCost =
                                    SafeGetDecimal(reader, "ACTUAL_COST"),

                                StandardCost =
                                    SafeGetDecimal(reader, "STANDARD_COST"),

                                CompNm =
                                    SafeGetString(reader, "COMP_NM")
                            });
                    }
                }
            }

            return result;
        }
    }
}  
