//Consolidated Income & Expenditure Regional Statement(Report)

// File: IncomeExpenditureRegionDetailedRepository.cs
using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace MISReports_Api.Repositories
{
    public class IncomeExpenditureRegionDetailedRepository
    {
        private readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<IncomeExpenditureRegionDetailedModel> GetIncomeExpenditureRegionDetailedReport(
            string compId,
            string year,
            string month)
        {
            var result = new List<IncomeExpenditureRegionDetailedModel>();

            string sql = @"
                SELECT
                C.title_cd,
                C.ac_cd AS ACCOUNT,
                NVL(ROUND(SUM(L.cl_bal),2),0.00) AS ACTUAL,
                K.ac_Nm AS CATNAME,
                '' AS MAXREV,
                TL.title_nm AS CATCODE,
                SUBSTR(C.title_cd,1,1) AS CATFLAG,
                  (select comp_nm from glcompm
                     where comp_id = :compID )as comp_nm,
                     (select case when lvl_no = 60 then comp_id when lvl_no = 50 then parent_id
                 when lvl_no = 0 and comp_id != :compID then grp_comp else L.dept_id end from glcompm where comp_id in
                (select comp_id from gldeptm where dept_id = L.dept_id )) as costctr
                FROM glacgrpm C ,gltitlm TL, gldeptm , glacctm K,
                (glledgrm LM
                LEFT OUTER JOIN gllegbal L ON LM.gl_cd = L.gl_cd )
                WHERE LM.ac_cd = C.ac_cd AND C.title_cd = TL.title_cd and
                       L.dept_id = gldeptm.dept_id and
                  C.ac_cd = K.ac_cd
                AND ( L.dept_id IN (select dept_id from gldeptm where comp_id in (select comp_id
                from glcompm
                where comp_id = :compID or grp_comp = :compID or parent_id = :compID )))
                  AND L.yr_ind = :year
                          AND L.mth_ind = :month
                AND( C.title_cd like ( 'XP%') or C.title_cd like ( 'IN%' ))
                Group by 7,C.title_cd ,C.ac_cd,K.ac_Nm, ( case when gldeptm.comp_id = :compID then gldeptm.dept_id else substr(gldeptm.dept_id,1,3) end ) ,L.dept_id ,TL.title_nm
                order BY 7,1,2,4 ,8";

            using (OracleConnection conn = new OracleConnection(_connectionString))
            using (OracleCommand cmd = new OracleCommand(sql, conn))
            {
                cmd.BindByName = true;
                cmd.Parameters.Add("compID", OracleDbType.Varchar2).Value = compId?.Trim() ?? "";
                cmd.Parameters.Add("year", OracleDbType.Varchar2).Value = year?.Trim() ?? "";
                cmd.Parameters.Add("month", OracleDbType.Varchar2).Value = month?.Trim() ?? "";

                conn.Open();
                using (OracleDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var model = new IncomeExpenditureRegionDetailedModel
                        {
                            Title_cd = reader["title_cd"].ToString(),
                            Account = reader["ACCOUNT"].ToString(),
                            Actual = reader["ACTUAL"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["ACTUAL"]),
                            Catname = reader["CATNAME"].ToString(),
                            Maxrev = reader["MAXREV"].ToString(), // Will always be empty
                            Catcode = reader["CATCODE"].ToString(),
                            Catflag = reader["CATFLAG"].ToString(),
                            Comp_nm = reader["comp_nm"].ToString(),
                            Costctr = reader["costctr"].ToString()
                        };
                        result.Add(model);
                    }
                }
            }

            return result;
        }
    }
}