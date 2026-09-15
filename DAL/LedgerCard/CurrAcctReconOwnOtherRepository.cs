using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class CurrAcctReconOwnOtherRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CurrAcctReconOwnOtherModel> GetData(string compId, int repyear, int repmonth, string subac)
        {
            var result = new List<CurrAcctReconOwnOtherModel>();

            string sql = @"
select 'Own Division' as CatCode,
       t1.sub_ac as SubAc,
       sum(T1.cl_bal) as ClBal,
       (select distinct gl_nm from glledgrm where gl_cd like '%L9200%' and rownum = 1) AS AcName,
       (select comp_nm from glcompm where trim(comp_id) = trim(:COMPID)) AS CctName
from glsubbal T1, glsubacm T2
where T1.gl_cd like '%L9200%'
  and T2.status = 2
  and T1.gl_cd = T2.gl_cd
  and T1.sub_ac = T2.sub_ac
  and T1.yr_ind = :REPYEAR
  and T1.mth_ind = :REPMONTH
  and T1.dept_id in (
      select dept_id from gldeptm where status = 2 and comp_id in (
          select comp_id from glcompm where trim(comp_id) = trim(:COMPID) or trim(parent_id) = trim(:COMPID) or trim(grp_comp) = trim(:COMPID)
      )
  )
  and T1.cl_bal != 0
group by t1.sub_ac

union all

select 'Other Division' as CatCode,
       (case 
            when (b.comp_id = 'GENE' or b.parent_id = 'GENE' or b.grp_comp = 'GENE') then 'GENE'
            when (b.comp_id = 'TRANS' or b.parent_id = 'NSO' or b.grp_comp = 'NSO') then 'NSO'
            when (b.comp_id = 'AGMAM' or b.parent_id = 'NTNSP' or b.grp_comp = 'NTNSP') then 'NTNSP'
            when (b.comp_id = 'AFMHQ' or b.parent_id = 'AFMHQ' or b.grp_comp = 'AFMHQ') then 'AFMHQ'
            when (b.comp_id = 'DISCO1' or b.parent_id = 'DISCO1' or b.grp_comp = 'DISCO1') then 'DISCO1'
            when (b.comp_id = 'DISCO2' or b.parent_id = 'DISCO2' or b.grp_comp = 'DISCO2') then 'DISCO2'
            when (b.comp_id = 'DISCO3' or b.parent_id = 'DISCO3' or b.grp_comp = 'DISCO3') then 'DISCO3'
            when (b.comp_id = 'DISCO4' or b.parent_id = 'DISCO4' or b.grp_comp = 'DISCO4') then 'DISCO4'
            else 'No company' 
        end) as SubAc,
       sum(T1.cl_bal) as ClBal,
       (select distinct gl_nm from glledgrm where gl_cd like '%L9200%' and rownum = 1) AS AcName,
       (select comp_nm from glcompm where trim(comp_id) = trim(:COMPID)) AS CctName
from glsubbal T1, glsubacm T2, gldeptm a, glcompm b
where T1.gl_cd like '%L9200%'
  and T2.status = 2
  and T1.gl_cd = T2.gl_cd
  and T1.sub_ac = T2.sub_ac
  and T1.yr_ind = :REPYEAR
  and T1.mth_ind = :REPMONTH
  and trim(t1.sub_ac) = trim(:SUBAC)
  and a.status = 2
  and b.status = 2
  and a.comp_id = b.comp_id
  and b.lvl_no < 90
  and T1.dept_id = a.dept_id
  and T1.dept_id not in (
      select dept_id from gldeptm where status = 2 and comp_id in (
          select comp_id from glcompm where trim(comp_id) = trim(:COMPID) or trim(parent_id) = trim(:COMPID) or trim(grp_comp) = trim(:COMPID)
      )
  )
  and T1.cl_bal != 0
group by (case 
            when (b.comp_id = 'GENE' or b.parent_id = 'GENE' or b.grp_comp = 'GENE') then 'GENE'
            when (b.comp_id = 'TRANS' or b.parent_id = 'NSO' or b.grp_comp = 'NSO') then 'NSO'
            when (b.comp_id = 'AGMAM' or b.parent_id = 'NTNSP' or b.grp_comp = 'NTNSP') then 'NTNSP'
            when (b.comp_id = 'AFMHQ' or b.parent_id = 'AFMHQ' or b.grp_comp = 'AFMHQ') then 'AFMHQ'
            when (b.comp_id = 'DISCO1' or b.parent_id = 'DISCO1' or b.grp_comp = 'DISCO1') then 'DISCO1'
            when (b.comp_id = 'DISCO2' or b.parent_id = 'DISCO2' or b.grp_comp = 'DISCO2') then 'DISCO2'
            when (b.comp_id = 'DISCO3' or b.parent_id = 'DISCO3' or b.grp_comp = 'DISCO3') then 'DISCO3'
            when (b.comp_id = 'DISCO4' or b.parent_id = 'DISCO4' or b.grp_comp = 'DISCO4') then 'DISCO4'
            else 'No company' 
        end)
ORDER BY 1 desc, 2";

            using (OracleConnection con = new OracleConnection(connectionString))
            {
                using (OracleCommand cmd = new OracleCommand(sql, con))
                {
                    cmd.BindByName = true;
                    cmd.Parameters.Add(new OracleParameter("COMPID", OracleDbType.Varchar2)).Value = compId;
                    cmd.Parameters.Add(new OracleParameter("REPYEAR", OracleDbType.Int32)).Value = repyear;
                    cmd.Parameters.Add(new OracleParameter("REPMONTH", OracleDbType.Int32)).Value = repmonth;
                    cmd.Parameters.Add(new OracleParameter("SUBAC", OracleDbType.Varchar2)).Value = subac;

                    con.Open();
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var item = new CurrAcctReconOwnOtherModel
                            {
                                CatCode = reader["CatCode"] != DBNull.Value ? reader["CatCode"].ToString() : string.Empty,
                                SubAc = reader["SubAc"] != DBNull.Value ? reader["SubAc"].ToString() : string.Empty,
                                ClBal = reader["ClBal"] != DBNull.Value ? Convert.ToDecimal(reader["ClBal"]) : (decimal?)null,
                                AcName = reader["AcName"] != DBNull.Value ? reader["AcName"].ToString() : string.Empty,
                                CctName = reader["CctName"] != DBNull.Value ? reader["CctName"].ToString() : string.Empty,
                            };
                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }
    }
}
