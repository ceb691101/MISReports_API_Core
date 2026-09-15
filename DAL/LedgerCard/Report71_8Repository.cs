using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class Report71_8Repository
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<Report71_8Model> GetReport71_8Data(string compId, int repyear, int repmonth)
        {
            var result = new List<Report71_8Model>();

            string sql = @"
SELECT
    T1.gl_cd AS GlCd,
    T1.sub_ac AS SubAc,
    T2.remarks AS Remarks,
    T2.acct_dt AS AcctDt,
    T1.doc_pf AS DocPf,
    T1.doc_no AS DocNo,
    T2.ref_1 AS Ref1,
    T2.ref_2 AS Ref2,
    T2.chq_no AS ChqNo,
    T1.cr_amt AS CrAmt,
    T1.dr_amt AS DrAmt,
    T2.log_mth AS LogMth,
    (SELECT comp_nm FROM glcompm WHERE trim(comp_id) = trim(:compId)) AS CctName
FROM
    glvocdmt T1, glvochmt T2
WHERE
    T1.doc_no = T2.doc_no
    AND T1.batch_id = T2.batch_id
    AND T1.doc_pf = T2.doc_pf
    AND T1.dept_id = T2.dept_id
    AND T2.log_yr = :repyear
    AND T2.log_mth = :repmonth
    AND T2.status = 6
    AND T2.doc_pf LIKE '%(71/8)%'
    AND T1.dept_id IN (
        SELECT dept_id FROM gldeptm
        WHERE status = 2
        AND comp_id IN (
            SELECT comp_id FROM glcompm
            WHERE trim(comp_id) = trim(:compId)
               OR trim(parent_id) = trim(:compId)
               OR trim(grp_comp) = trim(:compId)
        )
    )
ORDER BY
    T1.dept_id,
    T1.gl_cd,
    T1.sub_ac,
    T1.doc_no";

            using (var connection = new OracleConnection(connectionString))
            using (var command = new OracleCommand(sql, connection))
            {
                command.BindByName = true;
                command.Parameters.Add("compId", OracleDbType.Varchar2).Value = compId ?? string.Empty;
                command.Parameters.Add("repyear", OracleDbType.Int32).Value = repyear;
                command.Parameters.Add("repmonth", OracleDbType.Int32).Value = repmonth;

                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new Report71_8Model
                            {
                                GlCd = SafeGetString(reader, "GlCd"),
                                SubAc = SafeGetString(reader, "SubAc"),
                                Remarks = SafeGetString(reader, "Remarks"),
                                AcctDt = SafeGetDateTime(reader, "AcctDt"),
                                DocPf = SafeGetString(reader, "DocPf"),
                                DocNo = SafeGetString(reader, "DocNo"),
                                Ref1 = SafeGetString(reader, "Ref1"),
                                Ref2 = SafeGetString(reader, "Ref2"),
                                ChqNo = SafeGetString(reader, "ChqNo"),
                                CrAmt = SafeGetDecimal(reader, "CrAmt"),
                                DrAmt = SafeGetDecimal(reader, "DrAmt"),
                                LogMth = SafeGetInt32(reader, "LogMth"),
                                CctName = SafeGetString(reader, "CctName")
                            };
                            result.Add(item);
                        }
                    }
                }
                catch (OracleException oex)
                {
                    throw new Exception($"Oracle error in Report71_8Repository: Code {oex.Number} - {oex.Message}", oex);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error fetching 71/8 Report data: {ex.Message}", ex);
                }
            }

            return result;
        }

        #region Safe Readers
        private string SafeGetString(OracleDataReader reader, string columnName)
        {
            int colIndex = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(colIndex) ? reader.GetString(colIndex).Trim() : null;
        }

        private decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int colIndex = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(colIndex) ? reader.GetDecimal(colIndex) : (decimal?)null;
        }

        private DateTime? SafeGetDateTime(OracleDataReader reader, string columnName)
        {
            int colIndex = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(colIndex) ? reader.GetDateTime(colIndex) : (DateTime?)null;
        }

        private int? SafeGetInt32(OracleDataReader reader, string columnName)
        {
            int colIndex = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(colIndex) ? reader.GetInt32(colIndex) : (int?)null;
        }
        #endregion
    }
}
