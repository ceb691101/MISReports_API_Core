using MISReports_Api.Models.PhysicalVerification;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MISReports_Api.DAL.PhysicalVerification
{
    public class PHVValidationWarehousewiseRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

      
        private string SanitizeXmlString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");
        }

        public async Task<List<PHVValidationWarehousewiseModel>> GetWarehousewiseValidationAsync(
            string deptId,
            string warehouseCode,
            int repYear,
            int repMonth)
        {
            var result = new List<PHVValidationWarehousewiseModel>();
            string sql = @"
                    SELECT DISTINCT
    TRIM(T3.WRH_CD)   AS WRH_CD,
    TRIM(T1.MAT_CD)   AS MAT_CD,
    TRIM(T2.MAT_NM)   AS MAT_NM,
    TRIM(T4.UOM_CD)   AS UOM_CD,
    TRIM(T4.GRADE_CD) AS GRADE_CD,
    T1.QTY_ON_HAND,
    T1.CNTED_QTY,
    T4.UNIT_PRICE,
    TRIM(T1.REASON)   AS REASON
FROM
    INPHVDTT T1
    JOIN INMATM   T2 ON T1.MAT_CD   = T2.MAT_CD
    JOIN INPHVHTT T3 ON T3.DOC_NO   = T1.DOC_NO AND T3.DOC_PF = T1.DOC_PF
    JOIN INWRHMTM T4 ON T1.MAT_CD   = T4.MAT_CD 
                    AND T1.GRADE_CD = T4.GRADE_CD 
                    AND T3.WRH_CD   = T4.WRH_CD
WHERE
    TRIM(T3.DEPT_ID) = :dept_id
    AND TRIM(T4.DEPT_ID) = :dept_id
    AND TRIM(T1.DEPT_ID) = :dept_id
    AND TRIM(T3.WRH_CD)  = :wrh_cd
    AND TO_CHAR(T3.PHV_DT,'YYYY') = :rep_year
    AND TO_CHAR(T3.PHV_DT,'MM')   = :rep_month
    AND T4.STATUS IN (2,7) 
ORDER BY
    WRH_CD,
    MAT_CD,
    GRADE_CD";

            using (var conn = new OracleConnection(_connectionString))
            using (var cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("dept_id", OracleDbType.Varchar2).Value = deptId?.Trim();
                cmd.Parameters.Add("wrh_cd", OracleDbType.Varchar2).Value = warehouseCode?.Trim();
                cmd.Parameters.Add("rep_year", OracleDbType.Varchar2).Value = repYear.ToString();
                cmd.Parameters.Add("rep_month", OracleDbType.Varchar2).Value = repMonth.ToString("D2");

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new PHVValidationWarehousewiseModel
                        {
                            WarehouseCode = SanitizeXmlString(reader["WRH_CD"]?.ToString().Trim()),
                            MaterialCode = SanitizeXmlString(reader["MAT_CD"]?.ToString().Trim()),
                            MaterialName = SanitizeXmlString(reader["MAT_NM"]?.ToString().Trim()),
                            UomCode = SanitizeXmlString(reader["UOM_CD"]?.ToString().Trim()),
                            GradeCode = SanitizeXmlString(reader["GRADE_CD"]?.ToString().Trim()),
                            QtyOnHand = reader["QTY_ON_HAND"] != DBNull.Value
                                ? Convert.ToDecimal(reader["QTY_ON_HAND"])
                                : 0,
                            CountedQty = reader["CNTED_QTY"] != DBNull.Value
                                ? Convert.ToDecimal(reader["CNTED_QTY"])
                                : 0,
                            UnitPrice = reader["UNIT_PRICE"] != DBNull.Value
                                ? Convert.ToDecimal(reader["UNIT_PRICE"])
                                : 0,
                            Reason = SanitizeXmlString(reader["REASON"]?.ToString().Trim())
                        });
                    }
                }
            }

            return result;
        }
    }
}
