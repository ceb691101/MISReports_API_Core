using MISReports_Api.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace MISReports_Api.DAL
{
    public class CurrentAccountReconciliationExternalRepository
    {
        private readonly string connectionString = ConfigurationManager.ConnectionStrings["HQOracle"].ConnectionString;

        public List<CurrentAccountReconciliationExternalModel> GetData(string compId, int repyear, int repmonth, string subac)
        {
            var result = new List<CurrentAccountReconciliationExternalModel>();

            string sql = @"
select    t2.ent_dt as EntDt, T2.post_dt as PostDt ,T2.acct_dt as AcctDt,
            T1.sub_ac as SubAc,
           substr(T1.doc_no,1,6) as DeptId1,
             T1.dept_id as DeptId,
(select comp_id from gldeptm where dept_id= T1.dept_id) as ParentId,
(select comp_id from gldeptm where dept_id=  t2.trf_dept) as TrfParentId,
            T1.doc_pf as DocPf,
            T1.doc_no as DocNo,
            T2.ref_1 as Ref1,
            T2.ref_2 as Ref2,
            T2.remarks as Remarks,
           t2.trf_dept as DesgDept,
            T1.cr_amt as CrAmt,
            T1.dr_amt as DrAmt,
            T2.log_mth as LogMth,
(select     sum(T4.op_bal)
 from       glsubbal T4
 where      substr(T4.gl_cd,8,5) =  'L9200'  and
           trim(T4.sub_ac) = trim(:SUBAC) and
            T4.yr_ind=  :REPYEAR and T4.mth_ind=  :REPMONTH and t4.dept_id in  (select dept_id  from gldeptm  where status=2 and comp_id in (select comp_id
				  	      	from glcompm
				              	where trim(comp_id) = trim(:COMPID) or trim(parent_id) = trim(:COMPID)
or trim(grp_comp) = trim(:COMPID)))) as OpBal,
(select     sum(T4.cl_bal)
 from       glsubbal T4
 where      substr(T4.gl_cd,8,5) =  'L9200'  and
           trim(T4.sub_ac) = trim(:SUBAC) and
            T4.yr_ind=  :REPYEAR and T4.mth_ind= :REPMONTH and t4.dept_id in  (select dept_id  from gldeptm  where status=2 and comp_id in (select comp_id
				  	      	from glcompm
				              	where trim(comp_id) = trim(:COMPID) or trim(parent_id) = trim(:COMPID)
or trim(grp_comp) = trim(:COMPID)))) as ClBal,
(select comp_nm from glcompm where trim(comp_id) = trim(:COMPID)) as CompNm
             from       glvocdmt T1, glvochmt T2
 where      T1.doc_no=T2.doc_no and
            T1.batch_id=T2.batch_id and
            T1.doc_pf=T2.doc_pf and
            T1.dept_id=T2.dept_id and
            substr(T1.gl_cd,8,5) =  'L9200'  and
            trim(T1.sub_ac) = trim(:SUBAC) and
            T2.status = 6 and
            T2.log_yr = :REPYEAR and
            T2.log_mth =:REPMONTH and
T1.dept_id in  (select dept_id  from gldeptm  where status=2 and comp_id in (select comp_id
				  	      	from glcompm
				              	where trim(comp_id) = trim(:COMPID) or trim(parent_id) = trim(:COMPID)
or trim(grp_comp) = trim(:COMPID)))
ORDER   BY    T2.post_dt, T1.doc_no";

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
                            var item = new CurrentAccountReconciliationExternalModel
                            {
                                EntDt = reader["EntDt"] != DBNull.Value ? Convert.ToDateTime(reader["EntDt"]) : (DateTime?)null,
                                PostDt = reader["PostDt"] != DBNull.Value ? Convert.ToDateTime(reader["PostDt"]) : (DateTime?)null,
                                AcctDt = reader["AcctDt"] != DBNull.Value ? Convert.ToDateTime(reader["AcctDt"]) : (DateTime?)null,
                                SubAc = reader["SubAc"] != DBNull.Value ? reader["SubAc"].ToString() : string.Empty,
                                DeptId1 = reader["DeptId1"] != DBNull.Value ? reader["DeptId1"].ToString() : string.Empty,
                                DeptId = reader["DeptId"] != DBNull.Value ? reader["DeptId"].ToString() : string.Empty,
                                ParentId = reader["ParentId"] != DBNull.Value ? reader["ParentId"].ToString() : string.Empty,
                                TrfParentId = reader["TrfParentId"] != DBNull.Value ? reader["TrfParentId"].ToString() : string.Empty,
                                DocPf = reader["DocPf"] != DBNull.Value ? reader["DocPf"].ToString() : string.Empty,
                                DocNo = reader["DocNo"] != DBNull.Value ? reader["DocNo"].ToString() : string.Empty,
                                Ref1 = reader["Ref1"] != DBNull.Value ? reader["Ref1"].ToString() : string.Empty,
                                Ref2 = reader["Ref2"] != DBNull.Value ? reader["Ref2"].ToString() : string.Empty,
                                Remarks = reader["Remarks"] != DBNull.Value ? reader["Remarks"].ToString() : string.Empty,
                                DesgDept = reader["DesgDept"] != DBNull.Value ? reader["DesgDept"].ToString() : string.Empty,
                                CrAmt = reader["CrAmt"] != DBNull.Value ? Convert.ToDecimal(reader["CrAmt"]) : (decimal?)null,
                                DrAmt = reader["DrAmt"] != DBNull.Value ? Convert.ToDecimal(reader["DrAmt"]) : (decimal?)null,
                                LogMth = reader["LogMth"] != DBNull.Value ? Convert.ToInt32(reader["LogMth"]) : (int?)null,
                                OpBal = reader["OpBal"] != DBNull.Value ? Convert.ToDecimal(reader["OpBal"]) : (decimal?)null,
                                ClBal = reader["ClBal"] != DBNull.Value ? Convert.ToDecimal(reader["ClBal"]) : (decimal?)null,
                                CompNm = reader["CompNm"] != DBNull.Value ? reader["CompNm"].ToString() : string.Empty,
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
