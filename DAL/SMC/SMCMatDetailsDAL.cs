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
    public class SMCMatDetailsDAL
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

        // JobRegisterCCDAL/FundSummaryDAL.
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

        public List<SMCMatDetailsModel> GetSMCMatDetails(DateTime fromDate, DateTime toDate, string compId)
        {
            var result = new List<SMCMatDetailsModel>();

            const string query = @"
                SELECT    b.phase,
                          b.connection_type,
                          b.tariff_cat_code,
                          e.loop_cable,
                          d.wiring_type,
                          a.std_cost                    AS ACTUAL_COST,
                          d.total_cost                  AS STANDARD_COST,
                          a.project_no,
                          a1.res_cd,
                          SUM(a1.commited_qty)           AS QTY,
                          mat.mat_nm,
                          mat.maj_uom,
                          (SELECT comp_nm
                             FROM glcompm
                            WHERE comp_id IN (SELECT comp_id FROM gldeptm WHERE dept_id = a.dept_id)) AS AREA,
                          (SELECT comp_nm FROM glcompm WHERE TRIM(comp_id) = TRIM(:compid)) AS COMP_NM
                FROM      pcesthmt a
                JOIN      pcestdmt a1 ON TRIM(a.dept_id) = TRIM(a1.dept_id)
                                     AND TRIM(a.estimate_no) = TRIM(a1.estimate_no)
                JOIN      application_reference c ON TRIM(a.estimate_no) = TRIM(c.application_no)
                JOIN      wiring_land_detail b ON TRIM(b.application_id) = TRIM(c.application_id)
                JOIN      speststd d ON TRIM(a.estimate_no) = TRIM(d.estimate_no)
                JOIN      spserest e ON TRIM(e.application_no) = TRIM(c.application_no)
                JOIN      inmatm mat ON a1.res_cd = mat.mat_cd
                WHERE     a.prj_ass_dt >= :fromdate
                  AND     a.prj_ass_dt <  :todateexcl
                  AND     a1.res_cat = 1
                  AND     a.estimate_no LIKE '%ENC%'
                  AND     a.dept_id IN
                          (SELECT dept_id
                             FROM gldeptm
                            WHERE TRIM(comp_id) IN
                                  (SELECT comp_id
                                     FROM glcompm
                                    WHERE status = 2
                                      AND (TRIM(comp_id) = TRIM(:compid)
                                        OR TRIM(parent_id) = TRIM(:compid)
                                        OR TRIM(grp_comp) = TRIM(:compid))))
                GROUP BY  a.dept_id, b.phase, b.connection_type, b.tariff_cat_code, d.wiring_type,
                          e.loop_cable, a1.res_cd, mat.mat_nm, mat.maj_uom, a.std_cost, d.total_cost,
                          a.project_no, d.line_length
                ORDER BY  a.project_no, b.phase, b.connection_type, b.tariff_cat_code,
                          e.loop_cable, d.wiring_type, a1.res_cd";

            string compIdTrimmed = (compId ?? string.Empty).Trim();
            DateTime toDateExclusive = toDate.Date.AddDays(1);

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true; // required so :compid (used multiple times) binds correctly by name

                cmd.Parameters.Add(new OracleParameter("compid", OracleDbType.Varchar2) { Value = compIdTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromdate", OracleDbType.Date) { Value = fromDate.Date });
                cmd.Parameters.Add(new OracleParameter("todateexcl", OracleDbType.Date) { Value = toDateExclusive });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new SMCMatDetailsModel
                        {
                            Phase = SafeGetString(reader, "PHASE"),
                            ConnectionType = SafeGetString(reader, "CONNECTION_TYPE"),
                            TariffCatCode = SafeGetString(reader, "TARIFF_CAT_CODE"),
                            LoopCable = SafeGetString(reader, "LOOP_CABLE"),
                            WiringType = SafeGetString(reader, "WIRING_TYPE"),
                            ActualCost = SafeGetDecimal(reader, "ACTUAL_COST"),
                            StandardCost = SafeGetDecimal(reader, "STANDARD_COST"),
                            ProjectNo = SafeGetString(reader, "PROJECT_NO"),
                            ResCd = SafeGetString(reader, "RES_CD"),
                            Qty = SafeGetDecimal(reader, "QTY"),
                            MatNm = SafeGetString(reader, "MAT_NM"),
                            MajUom = SafeGetString(reader, "MAJ_UOM"),
                            Area = SafeGetString(reader, "AREA"),
                            CompNm = SafeGetString(reader, "COMP_NM")
                        });
                    }
                }
            }

            return result;
        }
    }
}