using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.DAL
{
    public class ProvinceIncomeExpenditureRepository
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<ProvinceIncomeExpenditureModel> GetProvinceIncomeExpenditure(
            string compId, string repYear, string repMonth)
        {
            var rows = new List<ProvinceIncomeExpenditureModel>();

            using (var conn = new OracleConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new OracleCommand(BuildNewQuery(), conn))
                {
                    cmd.BindByName = true;
                    cmd.CommandTimeout = 300;

                    // parameters (normalize once)
                    cmd.Parameters.Add("companyId", OracleDbType.Varchar2).Value = compId.Trim().ToUpper();
                    cmd.Parameters.Add("year", OracleDbType.Varchar2).Value = repYear.Trim();  // use varchar; cast in SQL
                    cmd.Parameters.Add("month", OracleDbType.Varchar2).Value = repMonth.Trim();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new ProvinceIncomeExpenditureModel
                            {
                                TitleCd = SafeGetString(reader, "TITLE_CD"),
                                Account = SafeGetString(reader, "ACCOUNT"),
                                Actual = SafeGetDecimal(reader, "ACTUAL"),
                                CatName = SafeGetString(reader, "CATNAME"),
                                MaxRev = "", // not supplied by query
                                CatCode = SafeGetString(reader, "CATCODE"),
                                CatFlag = SafeGetString(reader, "CATFLAG"),
                                AreaNum = SafeGetString(reader, "AREA_NUM"),
                                CctName = SafeGetString(reader, "CCT_NM"),
                            });
                        }
                    }
                }
            }

            return rows;
        }

        private string BuildNewQuery()
        {
            // NOTE:
            // - No trailing semicolon
            // - Explicit join to GLCOMPM as GC
            // - Normalize company & dept, explicit numeric casts for year/month
            return @"
SELECT
    C.title_cd,
    C.ac_cd AS ACCOUNT,
    NVL(ROUND(SUM(NVL(L.cl_bal,0)), 2), 0.00) AS ACTUAL,
    K.ac_nm AS CATNAME,
    '' AS MAXREV,
    TL.title_nm AS CATCODE,
    SUBSTR(C.title_cd, 1, 1) AS CATFLAG,
    CASE
        WHEN TRIM(UPPER(D.comp_id)) = :companyId THEN D.dept_id
        ELSE SUBSTR(D.dept_id, 1, 3)
    END AS AREA_NUM,
    D.dept_nm AS CCT_NM
FROM glacgrpm   C
JOIN gltitlm    TL ON TL.title_cd = C.title_cd
JOIN glacctm    K  ON K.ac_cd     = C.ac_cd
JOIN glledgrm   LM ON LM.ac_cd    = C.ac_cd
JOIN gllegbal   L  ON L.gl_cd     = LM.gl_cd
JOIN gldeptm    D  ON D.dept_id   = L.dept_id
JOIN glcompm    GC ON TRIM(UPPER(GC.comp_id)) = TRIM(UPPER(D.comp_id))
WHERE
      ( TRIM(UPPER(GC.comp_id))  = :companyId
        OR TRIM(UPPER(GC.parent_id)) = :companyId )
  AND L.yr_ind  = TO_NUMBER(:year)
  AND L.mth_ind = TO_NUMBER(:month)
  AND (C.title_cd LIKE 'XP%' OR C.title_cd LIKE 'IN%')
GROUP BY
    C.title_cd,
    C.ac_cd,
    K.ac_nm,
    TL.title_nm,
    CASE
        WHEN TRIM(UPPER(D.comp_id)) = :companyId THEN D.dept_id
        ELSE SUBSTR(D.dept_id, 1, 3)
    END,
    L.dept_id,
    D.dept_nm
ORDER BY AREA_NUM, C.title_cd, C.ac_cd, K.ac_nm, TL.title_nm";
        }

        private static string SafeGetString(OracleDataReader reader, string name)
        {
            var i = reader.GetOrdinal(name);
            return reader.IsDBNull(i) ? string.Empty : reader.GetString(i);
        }

        private static decimal SafeGetDecimal(OracleDataReader reader, string name)
        {
            var i = reader.GetOrdinal(name);
            if (reader.IsDBNull(i)) return 0m;

            // Some Oracle NUMBER columns may come back as decimal/double
            var obj = reader.GetValue(i);
            if (obj is decimal dec) return dec;
            if (obj is double dub) return Convert.ToDecimal(dub);
            if (obj is float flt) return Convert.ToDecimal(flt);
            if (obj is int ii) return ii;
            return Convert.ToDecimal(obj);
        }
    }
}