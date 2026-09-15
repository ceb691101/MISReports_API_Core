using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class IncomeExpenditureRegionRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<IncomeExpenditureRegionModel> GetIncomeExpenditureRegion(string companyId, int repYear, int repMonth)
        {
            var result = new List<IncomeExpenditureRegionModel>();

            const string sql = @"
                SELECT
                    C.title_cd AS TitleCd,
                    C.ac_cd AS Account,
                    NVL(ROUND(SUM(L.cl_bal), 2), 0.00) AS Actual,
                    K.ac_Nm AS CatName,
                    '' AS MaxRev,
                    TL.title_nm AS CatCode,
                    SUBSTR(C.title_cd, 1, 1) AS CatFlag,
                    (CASE
                        WHEN gldeptm.comp_id = :COMPANY_ID THEN gldeptm.dept_id
                        WHEN glcompm.parent_id = :COMPANY_ID THEN glcompm.comp_id
                        WHEN glcompm.grp_comp = :COMPANY_ID THEN glcompm.parent_id
                        ELSE ''
                     END) AS CostCtr,
                    (CASE
                        WHEN gldeptm.comp_id = :COMPANY_ID THEN gldeptm.dept_nm
                        WHEN glcompm.parent_id = :COMPANY_ID THEN glcompm.comp_nm
                        WHEN glcompm.grp_comp = :COMPANY_ID THEN
                            (SELECT DISTINCT a.comp_nm
                             FROM glcompm a
                             WHERE a.comp_id = glcompm.parent_id
                               AND a.status = 2)
                        ELSE ''
                     END) AS CompName,
                    gldeptm.dept_id AS DeptId
                FROM glacgrpm C
                INNER JOIN gltitlm TL ON C.title_cd = TL.title_cd
                INNER JOIN glledgrm LM ON LM.ac_cd = C.ac_cd
                LEFT OUTER JOIN gllegbal L ON LM.gl_cd = L.gl_cd
                INNER JOIN gldeptm ON L.dept_id = gldeptm.dept_id
                INNER JOIN glcompm ON glcompm.comp_id = gldeptm.comp_id
                INNER JOIN glacctm K ON C.ac_cd = K.ac_cd
                WHERE gldeptm.status = 2
                  AND glcompm.status = 2
                  AND L.dept_id IN (
                      SELECT dept_id
                      FROM gldeptm
                      WHERE status = 2
                        AND comp_id IN (
                            SELECT comp_id
                            FROM glcompm
                            WHERE status = 2
                              AND (Trim(comp_id) = :COMPANY_ID
                                OR Trim(parent_id) = :COMPANY_ID
                                OR Trim(grp_comp) = :COMPANY_ID)
                        )
                  )
                  AND L.yr_ind = :REPYEAR
                  AND L.mth_ind = :REPMONTH
                  AND (C.title_cd LIKE 'XP%' OR C.title_cd LIKE 'IN%')
                GROUP BY
                    glcompm.grp_comp, glcompm.parent_id, glcompm.comp_id,
                    gldeptm.comp_id, gldeptm.dept_id, gldeptm.dept_nm, glcompm.comp_nm,
                    C.title_cd, C.ac_cd, K.ac_Nm, TL.title_nm
                ORDER BY
                    glcompm.grp_comp, glcompm.parent_id, glcompm.comp_id,
                    gldeptm.comp_id, gldeptm.dept_id";

            using (var connection = new OracleConnection(connectionString))
            using (var command = new OracleCommand(sql, connection))
            {
                command.BindByName = true;
                command.Parameters.Add("COMPANY_ID", OracleDbType.Varchar2).Value = companyId.Trim();
                command.Parameters.Add("REPYEAR", OracleDbType.Int32).Value = repYear;
                command.Parameters.Add("REPMONTH", OracleDbType.Int32).Value = repMonth;

                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new IncomeExpenditureRegionModel
                            {
                                TitleCd = SafeGetString(reader, "TitleCd"),
                                Account = SafeGetString(reader, "Account"),
                                Actual = SafeGetDecimal(reader, "Actual") ?? 0m,
                                CatName = SafeGetString(reader, "CatName"),
                                MaxRev = SafeGetString(reader, "MaxRev"),
                                CatCode = SafeGetString(reader, "CatCode"),
                                CatFlag = SafeGetString(reader, "CatFlag"),
                                CostCtr = SafeGetString(reader, "CostCtr"),
                                CompName = SafeGetString(reader, "CompName"),
                                DeptId = SafeGetString(reader, "DeptId")
                            };
                            result.Add(item);
                        }
                    }
                }
                catch (OracleException oex)
                {
                    throw new Exception(
                        $"Oracle error fetching Income/Expenditure for Company {companyId}, Year {repYear}, Month {repMonth}. " +
                        $"Error Code: {oex.Number}, Message: {oex.Message}", oex);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"Error fetching Income/Expenditure Region data for Company {companyId}.", ex);
                }
            }

            return result;
        }

        // === Safe Data Access Helpers ===
        private string SafeGetString(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private decimal? SafeGetDecimal(OracleDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal);
        }
    }
}