using MISReports_Api.Models.FIFO;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.FIFO
{
    public class PHVDamageFIFORepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        private static string SanitizeXmlString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sanitized = input;
            foreach (var ch in sanitized.ToCharArray())
            {
                if (char.IsControl(ch) && ch != '\r' && ch != '\n' && ch != '\t')
                {
                    sanitized = sanitized.Replace(ch.ToString(), string.Empty);
                }
            }

            return sanitized.Trim();
        }

        public async Task<List<PHVObsoleteIdleFIFOModel>> GetPHVDamageFIFOAsync(
            string deptId,
            string warehouseCode,
            int repYear)
        {
            var result = new List<PHVObsoleteIdleFIFOModel>();

            string sql = @"
                SELECT
                    f.doc_no,
                    f.MAT_CD,
                    m.MAT_NM,
                    f.grade_cd,
                    f.doc_dt AS phv_dt,
                    SUM(CASE WHEN f.damage_count > 0 THEN f.damage_count ELSE f.COUNTED_QTY END) AS QTY_ON_HAND,
                    SUM(f.UNIT_PRICE * (CASE WHEN f.damage_count > 0 THEN f.damage_count ELSE f.COUNTED_QTY END)) AS StockBook,
                    f.REASON,
                    (SELECT dept_nm FROM gldeptm WHERE dept_id = :dept_id) AS cct_name
                FROM fifo_phv f, inmatm m
                WHERE TRIM(f.mat_cd) = TRIM(m.mat_cd)
                  AND (f.damage_count > 0 OR TRIM(f.GRADE_CD) = 'DAM' OR TRIM(f.GRADE_CD) = 'DMG')
                  AND TRIM(f.dept_id) = :dept_id
                  AND f.status = :rep_year
                  AND TRIM(f.wrh_cd) = :wrh_cd
                GROUP BY
                    f.doc_no,
                    f.MAT_CD,
                    CASE WHEN f.damage_count > 0 THEN f.damage_count ELSE f.COUNTED_QTY END,
                    m.MAT_NM,
                    f.grade_cd,
                    f.REASON,
                    f.doc_dt
                ORDER BY
                    f.doc_no,
                    f.MAT_CD";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = deptId.Trim();
                cmd.Parameters.Add("rep_year", OracleDbType.Int32).Value = repYear;
                cmd.Parameters.Add("wrh_cd", OracleDbType.Varchar2).Value = warehouseCode.Trim();

                await conn.OpenAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new PHVObsoleteIdleFIFOModel
                        {
                            DocumentNo = SanitizeXmlString(reader["DOC_NO"]?.ToString()),
                            MaterialCode = SanitizeXmlString(reader["MAT_CD"]?.ToString()),
                            MaterialName = SanitizeXmlString(reader["MAT_NM"]?.ToString()),
                            GradeCode = SanitizeXmlString(reader["GRADE_CD"]?.ToString()),
                            PhvDate = reader["PHV_DT"] != DBNull.Value
                                ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("PHV_DT"))
                                : null,
                            QtyOnHand = reader["QTY_ON_HAND"] != DBNull.Value
                                ? Convert.ToDecimal(reader["QTY_ON_HAND"])
                                : 0,
                            StockBook = reader["STOCKBOOK"] != DBNull.Value
                                ? Convert.ToDecimal(reader["STOCKBOOK"])
                                : 0,
                            Reason = SanitizeXmlString(reader["REASON"]?.ToString()),
                            CostCentreName = SanitizeXmlString(reader["CCT_NAME"]?.ToString())
                        });
                    }
                }
            }

            return result;
        }
    }
}
