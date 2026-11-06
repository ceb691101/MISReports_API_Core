
using MISReports_Api.Compat;
using MISReports_Api.Models.SMC__Management_Information;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace MISReports_Api.DAL.SMC__Management_Information
{
    public class ProvincewiseSMCJobMaterialDetailsDAL
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvincewiseSMCJobMaterialDetailsModel>
            GetProvincewiseSMCJobMaterialDetails(
                string compId,
                DateTime fromDate,
                DateTime toDate,
                string wiringType)
        {
            if (string.IsNullOrWhiteSpace(compId))
                throw new ArgumentException("Company ID is required.");

            if (toDate.Date < fromDate.Date)
                throw new ArgumentException(
                    "To date cannot be earlier than from date.");

            // ALL or an empty value means both OH and UG.
            string normalizedWiringType =
                string.IsNullOrWhiteSpace(wiringType) ||
                string.Equals(
                    wiringType.Trim(),
                    "ALL",
                    StringComparison.OrdinalIgnoreCase)
                    ? null
                    : wiringType.Trim().ToUpperInvariant();

            if (normalizedWiringType != null &&
                normalizedWiringType != "OH" &&
                normalizedWiringType != "UG")
            {
                throw new ArgumentException(
                    "Wiring type must be ALL, OH, or UG.",
                    nameof(wiringType));
            }

            string wiringTypeCondition =
                normalizedWiringType == null
                    ? string.Empty
                    : " AND UPPER(TRIM(d.wiring_type)) = :wiringType ";

            var result =
                new List<ProvincewiseSMCJobMaterialDetailsModel>();

            string sql = @"
                SELECT
                    b.phase AS PHASE,
                    b.connection_type AS CONNECTION_TYPE,
                    b.tariff_cat_code AS TARIFF_CAT_CODE,
                    e.loop_cable AS LOOP_CABLE,
                    d.wiring_type AS WIRING_TYPE,
                    d.line_length AS LINE_LENGTH,
                    d.service_length AS SERVICE_LENGTH,
                    a.std_cost AS ACTUAL_COST,
                    d.total_cost AS STANDARD_COST,
                    a.project_no AS PROJECT_NO,
                    a1.res_cd AS RES_CD,
                    SUM(a1.commited_qty) AS QTY,
                    mat.mat_nm AS MAT_NM,
                    mat.maj_uom AS MAJ_UOM,

                    (
                        SELECT gc.comp_nm
                        FROM glcompm gc
                        WHERE gc.comp_id IN (
                            SELECT gd.comp_id
                            FROM gldeptm gd
                            WHERE gd.dept_id = a.dept_id
                        )
                    ) AS AREA,

                    (
                        SELECT gc.comp_nm
                        FROM glcompm gc
                        WHERE TRIM(gc.comp_id) = :compId
                    ) AS COMP_NM

                FROM
                    pcesthmt a,
                    pcestdmt a1,
                    wiring_land_detail b,
                    application_reference c,
                    speststd d,
                    spserest e,
                    inmatm mat

                WHERE
                    TRIM(e.application_no) =
                        TRIM(c.application_no)

                    AND a.prj_ass_dt >= :fromDate

                    AND a.prj_ass_dt < :toDateExclusive

                    AND TRIM(a.estimate_no) =
                        TRIM(c.application_no)

                    AND TRIM(b.application_id) =
                        TRIM(c.application_id)

                    AND TRIM(a.estimate_no) =
                        TRIM(d.estimate_no)

                    AND a.dept_id = a1.dept_id

                    AND a.estimate_no = a1.estimate_no

                    AND a1.res_cat = 1

                    AND a1.res_cd = mat.mat_cd

                    AND a1.commited_qty > 0

                    AND a.estimate_no LIKE '%ENC%'

                    " + wiringTypeCondition + @"

                    AND a.dept_id IN (
                        SELECT gd.dept_id
                        FROM gldeptm gd
                        WHERE gd.comp_id IN (
                            SELECT gc.comp_id
                            FROM glcompm gc
                            WHERE gc.status = 2
                              AND (
                                  TRIM(gc.comp_id) = :compId
                                  OR TRIM(gc.parent_id) = :compId
                              )
                        )
                    )

                GROUP BY
                    a.dept_id,
                    b.phase,
                    b.connection_type,
                    b.tariff_cat_code,
                    d.wiring_type,
                    e.loop_cable,
                    a1.res_cd,
                    mat.mat_nm,
                    mat.maj_uom,
                    a.std_cost,
                    d.total_cost,
                    a.project_no,
                    d.line_length,
                    d.service_length

                ORDER BY
                    a.project_no,
                    b.phase,
                    b.connection_type,
                    b.tariff_cat_code,
                    e.loop_cable,
                    d.wiring_type,
                    a1.res_cd";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(
                    "compId",
                    OracleDbType.Varchar2
                ).Value = compId.Trim();

                cmd.Parameters.Add(
                    "fromDate",
                    OracleDbType.Date
                ).Value = fromDate.Date;

                // Exclusive upper bound includes the entire end date.
                cmd.Parameters.Add(
                    "toDateExclusive",
                    OracleDbType.Date
                ).Value = toDate.Date.AddDays(1);

                if (normalizedWiringType != null)
                {
                    cmd.Parameters.Add(
                        "wiringType",
                        OracleDbType.Varchar2,
                        20
                    ).Value = normalizedWiringType;
                }

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model =
                            new ProvincewiseSMCJobMaterialDetailsModel
                            {
                                Phase =
                                    GetString(reader, "PHASE"),

                                ConnectionType =
                                    GetString(reader, "CONNECTION_TYPE"),

                                TariffCatCode =
                                    GetString(reader, "TARIFF_CAT_CODE"),

                                LoopCable =
                                    GetString(reader, "LOOP_CABLE"),

                                WiringType =
                                    GetString(reader, "WIRING_TYPE"),

                                LineLength =
                                    GetNullableDecimal(
                                        reader, "LINE_LENGTH"),

                                ServiceLength =
                                    GetNullableDecimal(
                                        reader, "SERVICE_LENGTH"),

                                ActualCost =
                                    GetNullableDecimal(
                                        reader, "ACTUAL_COST"),

                                StandardCost =
                                    GetNullableDecimal(
                                        reader, "STANDARD_COST"),

                                ProjectNo =
                                    GetString(reader, "PROJECT_NO"),

                                ResCd =
                                    GetString(reader, "RES_CD"),

                                Qty =
                                    GetNullableDecimal(
                                        reader, "QTY") ?? 0m,

                                MaterialName =
                                    GetString(reader, "MAT_NM"),

                                MajorUom =
                                    GetString(reader, "MAJ_UOM"),

                                Area =
                                    GetString(reader, "AREA"),

                                CompName =
                                    GetString(reader, "COMP_NM")
                            };

                        result.Add(model);
                    }
                }
            }

            return result;
        }

        private static string GetString(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
                return null;

            // Read textual columns as strings, then remove invalid
            // XML control characters.
            string value = Convert.ToString(
                reader.GetValue(ordinal),
                CultureInfo.InvariantCulture);

            return CleanOracleString(value);
        }

        private static string CleanOracleString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return new string(
                value.Where(c =>
                    c == '\t' ||
                    c == '\n' ||
                    c == '\r' ||
                    (!char.IsControl(c) &&
                     !char.IsSurrogate(c))
                ).ToArray()
            ).Trim();
        }

        private static decimal? GetNullableDecimal(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
                return null;

            OracleDecimal oracleValue;

            try
            {
                // Avoid GetValue() for Oracle NUMBER columns.
                oracleValue = reader.GetOracleDecimal(ordinal);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "GetNullableDecimal failed (exception rethrown). columnName={columnName}", columnName);
                throw new InvalidOperationException(
                    "Failed to read Oracle numeric column '" +
                    columnName + "'.",
                    ex);
            }

            if (oracleValue.IsNull)
                return null;

            try
            {
                // Preserves decimal precision where representable.
                return oracleValue.Value;
            }
            catch (OverflowException)
            {
                // Fallback for values outside .NET decimal range.
                // This may lose precision.
                try
                {
                    double doubleValue = (double)oracleValue;

                    if (double.IsNaN(doubleValue) ||
                        double.IsInfinity(doubleValue))
                    {
                        throw new OverflowException(
                            "The numeric value is not finite.");
                    }

                    return Convert.ToDecimal(doubleValue);
                }
                catch (Exception ex) when (
                    ex is OverflowException ||
                    ex is InvalidCastException)
                {
                    logger.Error(ex, "GetNullableDecimal failed (exception rethrown). columnName={columnName}", columnName);
                    throw new InvalidOperationException(
                        "Oracle numeric column '" + columnName +
                        "' cannot be represented as a .NET decimal.",
                        ex);
                }
            }
        }
    }
}
