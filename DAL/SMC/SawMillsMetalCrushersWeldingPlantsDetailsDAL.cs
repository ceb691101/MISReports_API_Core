using MISReports_Api.Compat;
using MISReports_Api.Models.SMC;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace MISReports_Api.DAL.SMC
{
    public class SawMillsMetalCrushersWeldingPlantsDetailsDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"]
                .ConnectionString;

        public List<SawMillsMetalCrushersWeldingPlantsDetailsModel>
            GetSawMillsMetalCrushersWeldingPlantsDetails(
                DateTime fromDate,
                DateTime toDate)
        {
            if (toDate.Date < fromDate.Date)
            {
                throw new ArgumentException(
                    "To date cannot be earlier than from date.");
            }

            var result =
                new List<SawMillsMetalCrushersWeldingPlantsDetailsModel>();

            string sql = @"
                SELECT
                    a.prj_ass_dt AS PRJ_ASS_DT,
                    a.project_no AS PROJECT_NO,
                    b.phase AS PHASE,
                    b.connection_type AS CONNECTION_TYPE,
                    b.tariff_cat_code AS TARIFF_CAT_CODE,
                    d.line_length AS LINE_LENGTH,
                    d.service_length AS SERVICE_LENGTH,
                    d.inside_length AS INSIDE_LENGTH,
                    a.std_cost AS ACTUAL_COST,
                    d.total_cost AS CONSUMER_PAYABLE,
                    a.dept_id AS DEPT_ID,
                    b.welding_plant AS WELDING_PLANT,
                    b.metal_crusher AS METAL_CRUSHER,
                    b.saw_mills AS SAW_MILLS
                FROM
                    pcesthmt a,
                    wiring_land_detail b,
                    application_reference c,
                    speststd d
                WHERE
                    a.prj_ass_dt >= :fromDate
                    AND a.prj_ass_dt < :toDateExclusive

                    AND TRIM(a.estimate_no) =
                        TRIM(c.application_no)

                    AND TRIM(b.application_id) =
                        TRIM(c.application_id)

                    AND TRIM(a.estimate_no) =
                        TRIM(d.estimate_no)

                    AND a.estimate_no LIKE '%ENC%'

                    AND (
                        b.welding_plant > 0
                        OR b.metal_crusher > 0
                        OR b.saw_mills > 0
                    )
                ORDER BY
                    a.dept_id,
                    a.prj_ass_dt,
                    a.project_no,
                    b.phase,
                    b.connection_type,
                    b.tariff_cat_code,
                    a.std_cost DESC";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(
                    "fromDate",
                    OracleDbType.Date
                ).Value = fromDate.Date;

                cmd.Parameters.Add(
                    "toDateExclusive",
                    OracleDbType.Date
                ).Value = toDate.Date.AddDays(1);

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model =
                            new SawMillsMetalCrushersWeldingPlantsDetailsModel
                            {
                                PrjAssDt = GetNullableDateTime(
                                    reader, "PRJ_ASS_DT"),

                                ProjectNo = GetString(
                                    reader, "PROJECT_NO"),

                                Phase = GetString(
                                    reader, "PHASE"),

                                ConnectionType = GetString(
                                    reader, "CONNECTION_TYPE"),

                                TariffCatCode = GetString(
                                    reader, "TARIFF_CAT_CODE"),

                                LineLength = GetNullableDecimal(
                                    reader, "LINE_LENGTH"),

                                ServiceLength = GetNullableDecimal(
                                    reader, "SERVICE_LENGTH"),

                                InsideLength = GetNullableDecimal(
                                    reader, "INSIDE_LENGTH"),

                                ActualCost = GetNullableDecimal(
                                    reader, "ACTUAL_COST"),

                                ConsumerPayable = GetNullableDecimal(
                                    reader, "CONSUMER_PAYABLE"),

                                DeptId = GetString(
                                    reader, "DEPT_ID"),

                                WeldingPlant = GetNullableDecimal(
                                    reader, "WELDING_PLANT"),

                                MetalCrusher = GetNullableDecimal(
                                    reader, "METAL_CRUSHER"),

                                SawMills = GetNullableDecimal(
                                    reader, "SAW_MILLS")
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

            return Convert.ToString(
                reader.GetValue(ordinal),
                CultureInfo.InvariantCulture)?.Trim();
        }

        private static decimal? GetNullableDecimal(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
                return null;

            return Convert.ToDecimal(
                reader.GetValue(ordinal),
                CultureInfo.InvariantCulture);
        }

        private static DateTime? GetNullableDateTime(
            OracleDataReader reader,
            string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
                return null;

            return Convert.ToDateTime(
                reader.GetValue(ordinal),
                CultureInfo.InvariantCulture);
        }
    }
}