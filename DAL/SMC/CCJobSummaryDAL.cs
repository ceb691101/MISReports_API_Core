using MISReports_Api.Compat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using MISReports_Api.Models.Accounts;

namespace MISReports_Api.DAL
{
    public class CCJobSummaryDAL
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        // Overflow-safe decimal reader, same pattern used on the other reports (unbounded
        // NUMBER columns can exceed .NET decimal's ~28-29 digit range and make
        // Convert.ToDecimal throw).
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

        public List<CCJobSummaryModel> GetCCJobSummary(string repYear, string costCtr, string fromNo, string toNo)
        {
            var result = new List<CCJobSummaryModel>();

            const string query = @"
                SELECT  SUBSTR(T2.IS_REF, 8, 11) AS JOB_NUM,
                        (SELECT mat_nm FROM inmatm WHERE mat_cd = T1.MAT_CD) AS mat_nm,
                        T3.qty_on_hand,
                        T1.MAT_CD,
                        T1.UNIT_COST,
                        T1.TRX_TYPE,
                        T1.TRX_QTY
                FROM      INPOSTMT T1, INTRHMT T2, inwrhmtm T3
                WHERE     T2.STATUS IN (4, 6)
                  AND     SUBSTR(T1.DOC_NO, 12, 2) = SUBSTR(:repyear, 3, 2)
                  AND     T1.TRX_TYPE = 'ISSUE'
                  AND     T1.DEPT_ID = T2.DEPT_ID
                  AND     T1.DOC_PF = T2.DOC_PF
                  AND     T1.DOC_NO = T2.DOC_NO
                  AND     TRIM(T2.DEPT_ID) = TRIM(:costctr)
                  AND     SUBSTR(T2.IS_REF, 15, 4) >= :fromno
                  AND     SUBSTR(T2.IS_REF, 15, 4) <= :tono
                  AND     T2.IS_REF LIKE '%SMC%'
                  AND     T3.mat_cd = T1.MAT_CD
                  AND     T3.dept_id = T2.dept_id
                  AND     T3.status = 2
                  AND     T3.Grade_cd = 'NEW'";

            // Notes vs. the original query:
            // 1. TRIM() added around T2.DEPT_ID compared to the :costctr bind variable --
            //    same fix applied across the other reports, since dept_id is a
            //    fixed-length CHAR column elsewhere in this schema. T3.dept_id = T2.dept_id
            //    is a column-to-column comparison, not a bind variable, so it's unaffected.
            // 2. T3 (inwrhmtm) is joined without a warehouse-code (wrh_cd) filter. If a
            //    department has stock for the same material across more than one
            //    warehouse, this join can multiply each INPOSTMT/INTRHMT row once per
            //    matching warehouse row, inflating the result set. Left exactly as written
            //    since I can't verify from here whether wrh_cd is unique per
            //    dept_id+mat_cd+status+grade_cd in your data -- flagging in case you see
            //    duplicate-looking rows in the output.
            // 3. SUBSTR(...) filters (JOB_NUM extraction, doc_no/year comparison, IS_REF
            //    range and LIKE '%SMC%') are reproduced exactly as given.
            string costCtrTrimmed = (costCtr ?? string.Empty).Trim();
            string repYearTrimmed = (repYear ?? string.Empty).Trim();
            string fromNoTrimmed = (fromNo ?? string.Empty).Trim();
            string toNoTrimmed = (toNo ?? string.Empty).Trim();

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(query, conn))
            {
                cmd.CommandType = CommandType.Text;
                cmd.BindByName = true;

                cmd.Parameters.Add(new OracleParameter("repyear", OracleDbType.Varchar2) { Value = repYearTrimmed });
                cmd.Parameters.Add(new OracleParameter("costctr", OracleDbType.Varchar2) { Value = costCtrTrimmed });
                cmd.Parameters.Add(new OracleParameter("fromno", OracleDbType.Varchar2) { Value = fromNoTrimmed });
                cmd.Parameters.Add(new OracleParameter("tono", OracleDbType.Varchar2) { Value = toNoTrimmed });

                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CCJobSummaryModel
                        {
                            JobNum = SafeGetString(reader, "JOB_NUM"),
                            MatNm = SafeGetString(reader, "mat_nm"),
                            QtyOnHand = SafeGetDecimal(reader, "qty_on_hand"),
                            MatCd = SafeGetString(reader, "MAT_CD"),
                            UnitCost = SafeGetDecimal(reader, "UNIT_COST"),
                            TrxType = SafeGetString(reader, "TRX_TYPE"),
                            TrxQty = SafeGetDecimal(reader, "TRX_QTY")
                        });
                    }
                }
            }

            return result;
        }
    }
}